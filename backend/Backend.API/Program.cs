using Azure.Monitor.OpenTelemetry.AspNetCore;

using Backend.API.ApplicationCore.Configuration;
using Backend.API.ApplicationCore.Services;
using Backend.API.Configuration;
using Backend.API.Domain.Interfaces;
using Backend.API.HealthChecks;
using Backend.API.Infrastructure.LLM.Configuration;
using Backend.API.Infrastructure.LLM.Providers;
using Backend.API.Infrastructure.Persistence;
using Backend.API.Infrastructure.Search;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.RateLimiting;

/*
 * Backend API for PDF Q&A Application
 *
 * This application provides a REST API for asking questions about pre-processed PDF documents.
 * It uses Semantic Kernel to orchestrate:
 * - OpenAI for generating query embeddings (same model as Preprocessor for consistency)
 * - Groq API for LLM-based chat completion (zero-cost cloud inference)
 *
 * Key endpoints:
 * - POST /api/ask - Ask a question and get an AI-generated answer with sources
 * - GET /health/live - Liveness probe (always returns Healthy if app is running)
 * - GET /health/ready - Readiness probe (checks if embeddings loaded and dependencies available)
 */

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 1. Azure Key Vault Configuration (Production Only)
// ========================================
// In production, load secrets from Azure Key Vault using Managed Identity
// This allows secure access to secrets without storing them in code or config files
if (builder.Environment.IsProduction())
{
    var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
    if (!string.IsNullOrWhiteSpace(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(
            keyVaultUri,
            new Azure.Identity.DefaultAzureCredential());
    }
}

// ========================================
// 1.5 Application Insights Configuration (Production Only)
// ========================================
// Configure telemetry and monitoring for production
if (!builder.Environment.IsDevelopment())
{
    var appInsightsConnectionString =
        Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

    if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
    {
        builder.Services.AddOpenTelemetry()
            .UseAzureMonitor(options => { options.ConnectionString = appInsightsConnectionString; });

        // Configure logging levels for production to conserve 5GB free tier quota
        // This is already configured in appsettings.Production.json
    }
}

// ========================================
// 2. Configuration Loading
// ========================================
// Load BackendOptions from appsettings.json
var backendOptions = builder.Configuration
    .GetSection("BackendOptions")
    .Get<BackendOptions>();

if (backendOptions == null)
{
    throw new InvalidOperationException("BackendOptions configuration is missing");
}

// Configuration priority: Key Vault (prod) → Environment Variables → appsettings.json
// This allows for secure configuration in production without committing secrets
backendOptions = backendOptions with
{
    LlmProvider = LlmProviderExtensions.TryParse(
        builder.Configuration["BackendOptions:LlmProvider"] ?? Environment.GetEnvironmentVariable("LLM_PROVIDER"),
        out var parsedProvider)
        ? parsedProvider
        : backendOptions.LlmProvider,
    OpenAIApiKey = builder.Configuration["BackendOptions:OpenAIApiKey"]
                   ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                   ?? backendOptions.OpenAIApiKey,
    OpenAIChatModel = builder.Configuration["BackendOptions:OpenAIChatModel"]
                      ?? backendOptions.OpenAIChatModel,
    GroqApiKey = builder.Configuration["BackendOptions:GroqApiKey"]
                 ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
                 ?? backendOptions.GroqApiKey,
    EmbeddingsFilePath = Environment.GetEnvironmentVariable("EMBEDDINGS_PATH")
                         ?? backendOptions.EmbeddingsFilePath
};

// Validate the appropriate API key is set for the selected provider
if (backendOptions.LlmProvider == LlmProvider.OpenAI && string.IsNullOrWhiteSpace(backendOptions.OpenAIApiKey))
{
    Console.WriteLine("ERROR: LlmProvider is set to 'OpenAI' but OpenAIApiKey is not configured.");
    Console.WriteLine("  Set via: dotnet user-secrets set 'BackendOptions:OpenAIApiKey' 'sk-...'");
    Console.WriteLine("  Or environment variable: OPENAI_API_KEY");
}

if (backendOptions.LlmProvider == LlmProvider.Groq)
{
    if (string.IsNullOrWhiteSpace(backendOptions.OpenAIApiKey))
    {
        Console.WriteLine("WARNING: OpenAIApiKey is not set. Still required for embeddings generation.");
        Console.WriteLine("  Set via: dotnet user-secrets set 'BackendOptions:OpenAIApiKey' 'sk-...'");
        Console.WriteLine("  Or environment variable: OPENAI_API_KEY");
    }

    if (string.IsNullOrWhiteSpace(backendOptions.GroqApiKey))
    {
        Console.WriteLine("ERROR: LlmProvider is set to 'Groq' but GroqApiKey is not configured.");
        Console.WriteLine("  Set via: dotnet user-secrets set 'BackendOptions:GroqApiKey' 'gsk_...'");
        Console.WriteLine("  Or environment variable: GROQ_API_KEY");
    }
}

// Register configuration as a singleton for dependency injection
builder.Services.AddSingleton(backendOptions);

// ========================================
// 3. Semantic Kernel Configuration
// ========================================
// Only initialize Semantic Kernel if API keys are configured
// This allows the app to start without keys (health endpoints work, but /api/ask won't)
var hasApiKeys = !string.IsNullOrWhiteSpace(backendOptions.OpenAIApiKey) &&
                 (backendOptions.LlmProvider == LlmProvider.OpenAI ||
                  (backendOptions.LlmProvider == LlmProvider.Groq &&
                   !string.IsNullOrWhiteSpace(backendOptions.GroqApiKey)));

if (hasApiKeys)
{
    var kernelBuilder = Kernel.CreateBuilder();

    // Configure OpenAI for embeddings generation
    // IMPORTANT: Must use the same model as the Preprocessor for vector space compatibility
    // Using text-embedding-3-small for cost-effective embeddings (~$0.02 per 1M tokens)
#pragma warning disable SKEXP0010
    kernelBuilder.AddOpenAIEmbeddingGenerator(
        backendOptions.OpenAIEmbeddingModel,
        backendOptions.OpenAIApiKey);
#pragma warning restore SKEXP0010

    // Configure Chat Completion Service based on the selected provider
    switch (backendOptions.LlmProvider)
    {
        case LlmProvider.OpenAI:
            // Use OpenAI directly for chat completion
            Console.WriteLine($"Configuring OpenAI chat completion: {backendOptions.OpenAIChatModel}");

            kernelBuilder.AddOpenAIChatCompletion(
                backendOptions.OpenAIChatModel,
                backendOptions.OpenAIApiKey);
            break;

        case LlmProvider.Groq:
            // Use Groq via OpenAI-compatible API
            Console.WriteLine($"Configuring Groq chat completion: {backendOptions.GroqModel}");

            var groqHttpClient = new HttpClient
            {
                BaseAddress = new Uri(backendOptions.GroqApiUrl ?? "https://api.groq.com/openai/v1")
            };

            kernelBuilder.AddOpenAIChatCompletion(
                backendOptions.GroqModel ?? "llama-3.3-70b-versatile",
                backendOptions.GroqApiKey!,
                httpClient: groqHttpClient);
            break;
    }

    // Build the kernel and register it as a singleton
    var kernel = kernelBuilder.Build();
    builder.Services.AddSingleton(kernel);

    // Extract and register the embedding service separately
    // MemoryService needs direct access to IEmbeddingGenerator
    // to generate embeddings for search queries
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
    {
        var k = sp.GetRequiredService<Kernel>();
        return k.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    });

    // Extract and register the chat completion service separately
    // LLM providers need direct access to IChatCompletionService
    builder.Services.AddSingleton<IChatCompletionService>(sp =>
    {
        var k = sp.GetRequiredService<Kernel>();
        return k.GetRequiredService<IChatCompletionService>();
    });
}

// ========================================
// 4. DDD Layer Services Registration
// ========================================

// Register Configuration Interfaces (extracted from BackendOptions)
var openAiConfig = OpenAiConfiguration.FromBackendOptions(backendOptions);
var groqConfig = GroqConfiguration.FromBackendOptions(backendOptions);
var appOptions = ApplicationOptions.Create(backendOptions);

builder.Services.AddSingleton<IOpenAiConfiguration>(openAiConfig);
builder.Services.AddSingleton<IGroqConfiguration>(groqConfig);
builder.Services.AddSingleton(appOptions);

// Only register AI-dependent services if API keys are configured
if (hasApiKeys)
{
    // Domain layer services
    builder.Services.AddSingleton<Backend.API.Domain.Interfaces.IUserQuestionSanitizer,
        Backend.API.Domain.Services.UserQuestionSanitizer>();

    // Infrastructure layer - Repository
    builder.Services.AddSingleton<IDocumentRepository, FileBasedDocumentRepository>();

    // Infrastructure layer - Embedding generator adapter
    builder.Services.AddSingleton<Backend.API.Domain.Interfaces.IEmbeddingGenerator,
        Backend.API.Infrastructure.LLM.SemanticKernel.SemanticKernelEmbeddingGenerator>();

    // Infrastructure layer - Semantic search
    builder.Services.AddSingleton<ISemanticSearch, InMemorySemanticSearch>();

    // Infrastructure layer - LLM providers
    builder.Services.AddSingleton<OpenAiProvider>();
    builder.Services.AddSingleton<GroqProvider>();
    builder.Services.AddSingleton<LlmProviderFactory>();

    // Register selected LLM provider via factory
    builder.Services.AddSingleton<ILlmProvider>(sp =>
        sp.GetRequiredService<LlmProviderFactory>().CreateProvider());

    // ApplicationCore layer - Application services
    builder.Services.AddSingleton<IQuestionAnsweringService,
        QuestionAnsweringService>();
}

// ========================================
// 5. ASP.NET Core Services Configuration
// ========================================
// Controllers for REST API endpoints
builder.Services.AddControllers();

// HTTP Client Factory (used by health checks)
builder.Services.AddHttpClient();

// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks();
if (hasApiKeys)
{
    healthChecksBuilder.AddCheck<MemoryServiceHealthCheck>(
        "memory_service",
        tags: new[] { "ready" });
}

// Swagger/OpenAPI for API documentation (Development only)
// Note: Registered only in Development to avoid Microsoft.OpenApi version conflicts in production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

// CORS configuration for frontend
// Origins configured via appsettings.json or Azure App Service Configuration (BackendOptions__AllowedOrigins__0, etc.)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (backendOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(backendOptions.AllowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            // No origins configured - block all cross-origin requests
            policy.AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

// Rate limiting (built-in ASP.NET Core .NET 8+) - DoS protection
builder.Services.AddRateLimiter(limiterOptions =>
{
    limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10, // 10 requests per minute
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2 // Allow 2 requests to queue
            }));
});

// Kestrel request limits - DoS protection against large payloads
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024; // 10KB max request size
});

var app = builder.Build();

// ========================================
// 6. Initialize Repository (DDD Architecture)
// ========================================
// Load embeddings on startup (fail fast if there are configuration issues)
if (hasApiKeys)
{
    try
    {
        // Initialize DDD repository
        var repository = app.Services.GetRequiredService<IDocumentRepository>();
        await repository.InitializeAsync();
        Console.WriteLine($"✓ Document repository initialized with {repository.GetChunkCount()} chunks");
    }
    catch (Exception ex)
    {
        // Provide helpful error messages for common issues
        Console.WriteLine($"✗ Failed to initialize document repository: {ex.Message}");
        Console.WriteLine($"  Make sure:");
        Console.WriteLine($"  1. The embeddings file exists at: {backendOptions.EmbeddingsFilePath}");
        Console.WriteLine($"  2. OpenAI API key is set and valid");
        Console.WriteLine($"  3. Internet connectivity is available for OpenAI API");
        throw; // Re-throw to prevent an app from starting in a broken state
    }
}
else
{
    Console.WriteLine("⚠ API keys not configured - AI services disabled");
    Console.WriteLine("  Set GroqApiKey and OpenAIApiKey in user secrets or environment variables");
    Console.WriteLine("  Health endpoints will work, but /api/ask will return 503");
}

// ========================================
// 7. Middleware Pipeline Configuration
// ========================================
// Swagger UI only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS before authorization
app.UseCors();

// Enable rate limiting middleware - DoS protection
app.UseRateLimiter();

// Authorization middleware (currently no auth, but prepared for future)
app.UseAuthorization();

// Map controller endpoints
app.MapControllers();

// Map health check endpoints
// Liveness probe: Always returns Healthy if the app is running
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Don't run any health checks, just return Healthy
});

// Readiness probe: Checks if embeddings are loaded and external dependencies are available
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// ========================================
// 8. Startup Information
// ========================================
Console.WriteLine($"Backend API starting...");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Embeddings: {backendOptions.EmbeddingsFilePath}");
Console.WriteLine($"LLM Provider: {backendOptions.LlmProvider}");

switch (backendOptions.LlmProvider)
{
    case LlmProvider.OpenAI:
        Console.WriteLine($"OpenAI Chat Model: {backendOptions.OpenAIChatModel}");
        break;
    case LlmProvider.Groq:
        Console.WriteLine($"Groq Model: {backendOptions.GroqModel}");
        Console.WriteLine($"Groq API URL: {backendOptions.GroqApiUrl}");
        break;
}

Console.WriteLine($"OpenAI Embedding Model: {backendOptions.OpenAIEmbeddingModel}");

// Run the application
app.Run();