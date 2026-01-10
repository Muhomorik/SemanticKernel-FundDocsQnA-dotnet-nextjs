using CommandLine;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using OpenAI;

using System.ClientModel;

using Preprocessor;
using Preprocessor.CliOptions;
using Preprocessor.Extractors;
using Preprocessor.Outputs;
using Preprocessor.Services;

// If no arguments provided, default to 'json' verb with defaults
if (args.Length == 0)
{
    var logger = CreateLogger<Program>();
    logger.LogInformation("No arguments provided. Using default: 'json' verb with OpenAI provider.");
    logger.LogInformation("To see all options, run with --help or use 'json --help' or 'cosmosdb --help'");
    logger.LogInformation("");

    args = new[] { "json" };
}

// Parse command line arguments for verb-based commands
var result = await Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args)
    .MapResult(
        (JsonOptions opts) => RunJsonVerbAsync(opts),
        (CosmosDbOptions opts) => RunCosmosDbVerbAsync(opts),
        errors => Task.FromResult(1));

return result;

/// <summary>
/// Handles the 'json' verb - generates embeddings and saves to local JSON file.
/// </summary>
static async Task<int> RunJsonVerbAsync(JsonOptions opts)
{
    var logger = CreateLogger<Program>();
    logger.LogInformation("Running 'json' verb: Generate embeddings → save to local JSON file");

    // Validate options
    var errors = opts.Validate().ToList();
    if (errors.Count > 0)
    {
        foreach (var error in errors)
        {
            logger.LogError("Validation error: {Error}", error);
        }

        return 1;
    }

    // Resolve OpenAI API key
    var openAIApiKey = await ResolveOpenAIApiKeyAsync(opts, logger);
    if (opts.Provider == EmbeddingProvider.OpenAI && string.IsNullOrWhiteSpace(openAIApiKey))
    {
        return 1;
    }

    // Build services
    var services = BuildServiceCollection(opts, openAIApiKey);
    var serviceProvider = services.BuildServiceProvider();

    // Create JSON output handler
    var jsonOutput = new JsonEmbeddingOutput(
        opts.Output,
        opts.Append,
        serviceProvider.GetRequiredService<ILogger<JsonEmbeddingOutput>>());

    // Create processing options (data only)
    var processingOptions = new ProcessingOptions
    {
        Method = opts.Method,
        InputDirectory = opts.Input
    };

    // Log configuration
    LogConfiguration(serviceProvider.GetRequiredService<ILogger<Program>>(), opts, openAIApiKey);

    // Run preprocessor (pass output handler as separate parameter)
    var preprocessor = serviceProvider.GetRequiredService<PreprocessorService>();
    return await preprocessor.ProcessAsync(processingOptions, jsonOutput);
}

/// <summary>
/// Handles the 'cosmosdb' verb - generates embeddings and uploads to backend API (Cosmos DB).
/// </summary>
static async Task<int> RunCosmosDbVerbAsync(CosmosDbOptions opts)
{
    var logger = CreateLogger<Program>();
    logger.LogInformation("Running 'cosmosdb' verb: Generate embeddings → upload to backend API");

    // Validate options
    var errors = opts.Validate().ToList();
    if (errors.Count > 0)
    {
        foreach (var error in errors)
        {
            logger.LogError("Validation error: {Error}", error);
        }

        return 1;
    }

    // Resolve OpenAI API key
    var openAIApiKey = await ResolveOpenAIApiKeyAsync(opts, logger);
    if (opts.Provider == EmbeddingProvider.OpenAI && string.IsNullOrWhiteSpace(openAIApiKey))
    {
        return 1;
    }

    // Build services
    var services = BuildServiceCollection(opts, openAIApiKey);
    var serviceProvider = services.BuildServiceProvider();

    // Create HTTP client for Cosmos DB output
    var httpClient = new HttpClient();

    // Create Cosmos DB output handler
    var cosmosDbOutput = new CosmosDbEmbeddingOutput(
        httpClient,
        opts.Url,
        opts.EffectiveApiKey!,
        opts.Operation,
        opts.BatchSize,
        serviceProvider.GetRequiredService<ILogger<CosmosDbEmbeddingOutput>>());

    // Create processing options (data only)
    var processingOptions = new ProcessingOptions
    {
        Method = "pdfpig", // CosmosDB verb always uses pdfpig
        InputDirectory = opts.Input
    };

    // Log configuration
    LogConfiguration(serviceProvider.GetRequiredService<ILogger<Program>>(), opts, openAIApiKey);
    logger.LogInformation("Backend URL: {Url}", opts.Url);
    logger.LogInformation("Operation: {Operation}", opts.Operation);
    logger.LogInformation("Batch Size: {BatchSize}", opts.BatchSize);

    // Run preprocessor (pass output handler as separate parameter)
    var preprocessor = serviceProvider.GetRequiredService<PreprocessorService>();
    return await preprocessor.ProcessAsync(processingOptions, cosmosDbOutput);
}

/// <summary>
/// Resolves OpenAI API key from CLI argument, environment variable, or user secrets.
/// </summary>
static Task<string?> ResolveOpenAIApiKeyAsync(BaseEmbeddingOptions opts, ILogger logger)
{
    if (opts.Provider != EmbeddingProvider.OpenAI)
    {
        return Task.FromResult<string?>(null);
    }

    // Build configuration for user secrets
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>(true)
        .Build();

    var openAIApiKey = opts.OpenAIApiKey
                       ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                       ?? configuration["OpenAIApiKey"];

    if (string.IsNullOrWhiteSpace(openAIApiKey))
    {
        logger.LogError("ERROR: OpenAI provider requires an API key.");
        logger.LogError("Set via one of:");
        logger.LogError("  1. --openai-api-key argument");
        logger.LogError("  2. OPENAI_API_KEY environment variable");
        logger.LogError("  3. User secrets: dotnet user-secrets set \"OpenAIApiKey\" \"sk-...\"");
        logger.LogError("Get your API key from: https://platform.openai.com/api-keys");
        return Task.FromResult<string?>(null);
    }

    return Task.FromResult<string?>(openAIApiKey);
}

/// <summary>
/// Builds the service collection with common services (embedding, extractor, preprocessor).
/// </summary>
static IServiceCollection BuildServiceCollection(BaseEmbeddingOptions opts, string? openAIApiKey)
{
    var services = new ServiceCollection();

    // Configure logging
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    // Configure Semantic Kernel with provider-specific embedding generator
    services.AddSingleton(sp =>
    {
        var builder = Kernel.CreateBuilder();

        switch (opts.Provider)
        {
            case EmbeddingProvider.Ollama:
                builder.AddOllamaEmbeddingGenerator(
                    opts.EmbeddingModel,
                    new Uri(opts.EffectiveUrl));
                break;

            case EmbeddingProvider.LMStudio:
                // CRITICAL: Work around OpenAI BaseAddress bug by including /v1 in URL
                var lmStudioEndpoint = opts.EffectiveUrl.TrimEnd('/');
                if (!lmStudioEndpoint.EndsWith("/v1"))
                {
                    lmStudioEndpoint += "/v1";
                }

                // For custom endpoints, create OpenAI client manually and register as IEmbeddingGenerator
                builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
                {
                    var openAIClient = new OpenAIClient(
                        new ApiKeyCredential("lm-studio"), // LM Studio ignores this
                        new OpenAIClientOptions
                        {
                            Endpoint = new Uri(lmStudioEndpoint)
                        });

                    return openAIClient
                        .GetEmbeddingClient(opts.EmbeddingModel)
                        .AsIEmbeddingGenerator();
                });
                break;

            case EmbeddingProvider.OpenAI:
                // Use OpenAI's text-embedding-3-small for production compatibility
#pragma warning disable SKEXP0010
                builder.AddOpenAIEmbeddingGenerator(
                    opts.EmbeddingModel,
                    openAIApiKey!); // Null-checked before this point
#pragma warning restore SKEXP0010
                break;
        }

        return builder.Build();
    });

    // Register embedding generator service from kernel
    services.AddSingleton(sp =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        return kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    });

    // Register PDF extractor
    services.AddSingleton<IPdfExtractor, PdfPigExtractor>();

    // Register services
    services.AddSingleton<IChunkSanitizer, ChunkSanitizer>();
    services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
    services.AddSingleton<PreprocessorService>();

    return services;
}

/// <summary>
/// Logs configuration information for the preprocessor.
/// </summary>
static void LogConfiguration(ILogger logger, BaseEmbeddingOptions opts, string? openAIApiKey)
{
    logger.LogInformation("Starting Preprocessor");
    logger.LogInformation("Input: {Input}", opts.Input);
    logger.LogInformation("Provider: {Provider}", opts.Provider);
    logger.LogInformation("Endpoint URL: {Url}", opts.EffectiveUrl);
    logger.LogInformation("Embedding Model: {EmbeddingModel}", opts.EmbeddingModel);

    if (opts.Provider == EmbeddingProvider.OpenAI && !string.IsNullOrWhiteSpace(openAIApiKey))
    {
        var maskedKey = openAIApiKey!.Length > 8
            ? $"{openAIApiKey.Substring(0, 7)}...{openAIApiKey.Substring(openAIApiKey.Length - 4)}"
            : "sk-****";
        logger.LogInformation("OpenAI API Key: {ApiKey}", maskedKey);

        var keySource = opts.OpenAIApiKey != null
            ? "CLI argument"
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY") != null
                ? "Environment variable"
                : "User secrets";
        logger.LogInformation("API Key Source: {Source}", keySource);
    }
}

/// <summary>
/// Creates a logger for the specified type.
/// </summary>
static ILogger<T> CreateLogger<T>()
{
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    return loggerFactory.CreateLogger<T>();
}