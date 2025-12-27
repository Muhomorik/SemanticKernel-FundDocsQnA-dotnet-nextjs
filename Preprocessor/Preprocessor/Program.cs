using CommandLine;

using Microsoft.Extensions.AI;
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