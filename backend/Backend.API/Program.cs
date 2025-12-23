using Backend.API.Configuration;
using Backend.API.Services;

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

/*
 * Backend API for PDF Q&A Application
 *
 * This application provides a REST API for asking questions about pre-processed PDF documents.
 * It uses Semantic Kernel to orchestrate:
 * - Ollama for generating embeddings (same model as Preprocessor for consistency)
 * - Groq API for LLM-based chat completion (zero-cost cloud inference)
 *
 * Key endpoints:
 * - POST /api/ask - Ask a question and get an AI-generated answer with sources
 * - GET /api/health - Check if the API is ready and embeddings are loaded
 */

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 1. Configuration Loading
// ========================================
// Load BackendOptions from appsettings.json
var backendOptions = builder.Configuration
    .GetSection("BackendOptions")
    .Get<BackendOptions>();

if (backendOptions == null)
{
    throw new InvalidOperationException("BackendOptions configuration is missing");
}

// Environment variables override appsettings.json values
// This allows for secure configuration in production without committing secrets
backendOptions = backendOptions with
{
    GroqApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? backendOptions.GroqApiKey,
    EmbeddingsFilePath = Environment.GetEnvironmentVariable("EMBEDDINGS_PATH") ?? backendOptions.EmbeddingsFilePath
};

// Warn if Groq API key is missing (app will still start but /api/ask will fail)
if (string.IsNullOrWhiteSpace(backendOptions.GroqApiKey))
{
    Console.WriteLine(
        "WARNING: GroqApiKey is not set. Please set it in appsettings.json or via GROQ_API_KEY environment variable.");
}

// Register configuration as singleton for dependency injection
builder.Services.AddSingleton(backendOptions);

// ========================================
// 2. Semantic Kernel Configuration
// ========================================
var kernelBuilder = Kernel.CreateBuilder();

// Configure Ollama for embeddings generation
// IMPORTANT: Must use the same model (nomic-embed-text) as the Preprocessor
// This ensures query embeddings are in the same vector space as document embeddings
kernelBuilder.AddOllamaEmbeddingGenerator(
    backendOptions.EmbeddingModel,
    new Uri(backendOptions.OllamaUrl));

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

// ========================================
// 3. Application Services Registration
// ========================================
// MemoryService: Loads embeddings.json and performs semantic search
builder.Services.AddSingleton<IMemoryService, MemoryService>();

// QuestionAnsweringService: Orchestrates search + LLM to answer questions
builder.Services.AddSingleton<IQuestionAnsweringService, QuestionAnsweringService>();

// ========================================
// 4. ASP.NET Core Services Configuration
// ========================================
// Controllers for REST API endpoints
builder.Services.AddControllers();

// Swagger/OpenAPI for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configuration for Next.js frontend
// Allows requests from localhost:3000 (Next.js dev) and localhost:3001 (alternative port)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// ========================================
// 5. Initialize Memory Service
// ========================================
// Load embeddings on startup and verify Ollama connectivity
// This is done eagerly so we fail fast if there are configuration issues
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
    Console.WriteLine($"  2. Ollama is running at: {backendOptions.OllamaUrl}");
    Console.WriteLine($"  3. The model '{backendOptions.EmbeddingModel}' is available in Ollama");
    throw; // Re-throw to prevent app from starting in a broken state
}

// ========================================
// 6. Middleware Pipeline Configuration
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

// ========================================
// 7. Startup Information
// ========================================
Console.WriteLine($"Backend API starting...");
Console.WriteLine($"Embeddings: {backendOptions.EmbeddingsFilePath}");
Console.WriteLine($"Groq Model: {backendOptions.GroqModel}");
Console.WriteLine($"Embedding Model: {backendOptions.EmbeddingModel}");

// Run the application
app.Run();