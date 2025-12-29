using Azure.Monitor.OpenTelemetry.AspNetCore;

using Backend.API.Configuration;
using Backend.API.HealthChecks;
using Backend.API.Services;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

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
    GroqApiKey = builder.Configuration["BackendOptions:GroqApiKey"]
                 ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
                 ?? backendOptions.GroqApiKey,
    OpenAIApiKey = builder.Configuration["BackendOptions:OpenAIApiKey"]
                   ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                   ?? backendOptions.OpenAIApiKey,
    EmbeddingsFilePath = Environment.GetEnvironmentVariable("EMBEDDINGS_PATH")
                         ?? backendOptions.EmbeddingsFilePath
};

// Warn if API keys are missing (app will still start but /api/ask will fail)
if (string.IsNullOrWhiteSpace(backendOptions.GroqApiKey))
{
    Console.WriteLine(
        "WARNING: GroqApiKey is not set. Please set it in appsettings.json or via GROQ_API_KEY environment variable.");
}

if (string.IsNullOrWhiteSpace(backendOptions.OpenAIApiKey))
{
    Console.WriteLine(
        "WARNING: OpenAIApiKey is not set. Please set it in appsettings.json or via OPENAI_API_KEY environment variable.");
}

// Register configuration as singleton for dependency injection
builder.Services.AddSingleton(backendOptions);

// ========================================
// 3. Semantic Kernel Configuration
// ========================================
// Only initialize Semantic Kernel if API keys are configured
// This allows the app to start without keys (health endpoints work, but /api/ask won't)
var hasApiKeys = !string.IsNullOrWhiteSpace(backendOptions.GroqApiKey) &&
                 !string.IsNullOrWhiteSpace(backendOptions.OpenAIApiKey);

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

    // Configure Groq for chat completion (LLM)
    // Groq provides OpenAI-compatible API, so we use AddOpenAIChatCompletion
    // with a custom HttpClient pointing to Groq's endpoint
    var groqHttpClient = new HttpClient
    {
        BaseAddress = new Uri(backendOptions.GroqApiUrl)
    };

    kernelBuilder.AddOpenAIChatCompletion(
        backendOptions.GroqModel,
        backendOptions.GroqApiKey,
        httpClient: groqHttpClient);

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
}

// ========================================
// 4. Application Services Registration
// ========================================
// Only register AI-dependent services if API keys are configured
if (hasApiKeys)
{
    // MemoryService: Loads embeddings.json and performs semantic search
    builder.Services.AddSingleton<IMemoryService, MemoryService>();

    // QuestionAnsweringService: Orchestrates search + LLM to answer questions
    builder.Services.AddSingleton<IQuestionAnsweringService, QuestionAnsweringService>();
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

var app = builder.Build();

// ========================================
// 6. Initialize Memory Service
// ========================================
// Load embeddings on startup (fail fast if there are configuration issues)
if (hasApiKeys)
{
    try
    {
        var memoryService = app.Services.GetRequiredService<IMemoryService>();
        await memoryService.InitializeAsync();
        Console.WriteLine($"✓ Memory service initialized with {memoryService.GetEmbeddingCount()} embeddings");
    }
    catch (Exception ex)
    {
        // Provide helpful error messages for common issues
        Console.WriteLine($"✗ Failed to initialize memory service: {ex.Message}");
        Console.WriteLine($"  Make sure:");
        Console.WriteLine($"  1. The embeddings file exists at: {backendOptions.EmbeddingsFilePath}");
        Console.WriteLine($"  2. OpenAI API key is set and valid");
        Console.WriteLine($"  3. Internet connectivity is available for OpenAI API");
        throw; // Re-throw to prevent app from starting in a broken state
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
Console.WriteLine($"Groq Model: {backendOptions.GroqModel}");
Console.WriteLine($"OpenAI Embedding Model: {backendOptions.OpenAIEmbeddingModel}");

// Run the application
app.Run();