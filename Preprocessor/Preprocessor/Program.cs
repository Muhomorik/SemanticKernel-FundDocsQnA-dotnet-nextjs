using CommandLine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

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

    // Configure Semantic Kernel with Ollama
    services.AddSingleton(sp =>
    {
        var builder = Kernel.CreateBuilder();

        builder.AddOllamaEmbeddingGenerator(
            cliOptions.EmbeddingModel,
            new Uri(cliOptions.OllamaUrl));

        // Add Ollama chat completion for vision (if needed)
        if (cliOptions.Method.Equals("ollama-vision", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddOllamaChatCompletion(
                cliOptions.VisionModel,
                new Uri(cliOptions.OllamaUrl));
        }

        return builder.Build();
    });


    // Register embedding generator service from kernel (new API)
    services.AddSingleton(sp =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        return kernel
            .GetRequiredService<
                Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
    });

    // Register chat completion service and extractor only for ollama-vision method
    if (cliOptions.Method.Equals("ollama-vision", StringComparison.OrdinalIgnoreCase))
    {
        services.AddSingleton(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            return kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
        });
        services.AddSingleton<IPdfExtractor>(sp => new OllamaVisionExtractor(
            sp.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(),
            sp.GetRequiredService<ILogger<OllamaVisionExtractor>>(),
            cliOptions.VisionModel));
    }
    else
    {
        services.AddSingleton<IPdfExtractor, PdfPigExtractor>();
    }

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
    logger.LogInformation("Ollama URL: {OllamaUrl}", cliOptions.OllamaUrl);
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