using CommandLine;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using OpenAI;

using System.ClientModel;

using Preprocessor;
using Preprocessor.Extractors;
using Preprocessor.Services;

// Parse command line arguments
var result = await Parser.Default.ParseArguments<CliOptions>(args)
    .MapResult(
        async options => await RunAsync(options),
        errors => Task.FromResult(1));

return result;

static async Task<int> RunAsync(CliOptions cliOptions)
{
    // Build configuration for user secrets
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>(optional: true)
        .Build();

    // Resolve OpenAI API key with priority: CLI argument → Environment variable → User secrets → Error
    string? openAIApiKey = null;
    if (cliOptions.Provider == EmbeddingProvider.OpenAI)
    {
        openAIApiKey = cliOptions.OpenAIApiKey
                       ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                       ?? configuration["OpenAIApiKey"];

        if (string.IsNullOrWhiteSpace(openAIApiKey))
        {
            Console.Error.WriteLine("ERROR: OpenAI provider requires an API key.");
            Console.Error.WriteLine("Set via one of:");
            Console.Error.WriteLine("  1. --openai-api-key argument");
            Console.Error.WriteLine("  2. OPENAI_API_KEY environment variable");
            Console.Error.WriteLine("  3. User secrets: dotnet user-secrets set \"OpenAIApiKey\" \"sk-...\"");
            Console.Error.WriteLine("Get your API key from: https://platform.openai.com/api-keys");
            return 1;
        }
    }

    // Build service collection
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

        switch (cliOptions.Provider)
        {
            case EmbeddingProvider.Ollama:
                builder.AddOllamaEmbeddingGenerator(
                    cliOptions.EmbeddingModel,
                    new Uri(cliOptions.EffectiveUrl));
                break;

            case EmbeddingProvider.LMStudio:
                // CRITICAL: Work around OpenAI BaseAddress bug by including /v1 in URL
                var lmStudioEndpoint = cliOptions.EffectiveUrl.TrimEnd('/');
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
                        .GetEmbeddingClient(cliOptions.EmbeddingModel)
                        .AsIEmbeddingGenerator();
                });
                break;

            case EmbeddingProvider.OpenAI:
                // Use OpenAI's text-embedding-3-small for production compatibility
                #pragma warning disable SKEXP0010
                builder.AddOpenAIEmbeddingGenerator(
                    cliOptions.EmbeddingModel,
                    openAIApiKey);
                #pragma warning restore SKEXP0010
                break;
        }

        return builder.Build();
    });


    // Register embedding generator service from kernel (new API)
    services.AddSingleton(sp =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        return kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    });

    // Register PDF extractor
    services.AddSingleton<IPdfExtractor, PdfPigExtractor>();

    // Register services
    services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
    services.AddSingleton<PreprocessorService>();

    // Build service provider
    var serviceProvider = services.BuildServiceProvider();

    // Get the preprocessor service and run
    var preprocessor = serviceProvider.GetRequiredService<PreprocessorService>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting Preprocessor");
    logger.LogInformation("Method: {Method}", cliOptions.Method);
    logger.LogInformation("Input: {Input}", cliOptions.Input);
    logger.LogInformation("Output: {Output}", cliOptions.Output);
    logger.LogInformation("Provider: {Provider}", cliOptions.Provider);
    logger.LogInformation("Endpoint URL: {Url}", cliOptions.EffectiveUrl);
    logger.LogInformation("Embedding Model: {EmbeddingModel}", cliOptions.EmbeddingModel);

    if (cliOptions.Provider == EmbeddingProvider.OpenAI)
    {
        var maskedKey = openAIApiKey!.Length > 8
            ? $"{openAIApiKey.Substring(0, 7)}...{openAIApiKey.Substring(openAIApiKey.Length - 4)}"
            : "sk-****";
        logger.LogInformation("OpenAI API Key: {ApiKey}", maskedKey);
        var keySource = cliOptions.OpenAIApiKey != null
            ? "CLI argument"
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY") != null
                ? "Environment variable"
                : "User secrets";
        logger.LogInformation("API Key Source: {Source}", keySource);
    }

    var exitCode = await preprocessor.ProcessAsync(cliOptions);

    if (exitCode == 0)
    {
        logger.LogInformation("Preprocessing completed successfully");
    }
    else
    {
        logger.LogError("Preprocessing failed with exit code {ExitCode}", exitCode);
    }

    return exitCode;
}