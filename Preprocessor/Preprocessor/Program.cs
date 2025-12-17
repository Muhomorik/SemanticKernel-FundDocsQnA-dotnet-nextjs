using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Preprocessor;
using Preprocessor.Extractors;
using Preprocessor.Services;

// Parse command line arguments
var result = await Parser.Default.ParseArguments<Options>(args)
    .MapResult(
        async options => await RunAsync(options),
        errors => Task.FromResult(1));

return result;

static async Task<int> RunAsync(Options options)
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

        // Add Ollama embedding service
#pragma warning disable SKEXP0070 // Ollama connector is experimental
        builder.AddOllamaTextEmbeddingGeneration(
            modelId: options.EmbeddingModel,
            endpoint: new Uri(options.OllamaUrl));

        // Add Ollama chat completion for vision (if needed)
        if (options.Method.Equals("ollama-vision", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddOllamaChatCompletion(
                modelId: options.VisionModel,
                endpoint: new Uri(options.OllamaUrl));
        }
#pragma warning restore SKEXP0070

        return builder.Build();
    });

    // Register embedding generation service from kernel
    services.AddSingleton(sp =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        return kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>();
    });

    // Register chat completion service from kernel (for vision)
    services.AddSingleton(sp =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        return kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
    });

    // Register extractors
    services.AddSingleton<IPdfExtractor, PdfPigExtractor>();
    services.AddSingleton<IPdfExtractor>(sp => new OllamaVisionExtractor(
        sp.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(),
        sp.GetRequiredService<ILogger<OllamaVisionExtractor>>(),
        options.VisionModel));

    // Register services
    services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
    services.AddSingleton<PreprocessorService>();

    // Build service provider
    var serviceProvider = services.BuildServiceProvider();

    // Get the preprocessor service and run
    var preprocessor = serviceProvider.GetRequiredService<PreprocessorService>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting Preprocessor");
    logger.LogInformation("Method: {Method}", options.Method);
    logger.LogInformation("Input: {Input}", options.Input);
    logger.LogInformation("Output: {Output}", options.Output);
    logger.LogInformation("Ollama URL: {OllamaUrl}", options.OllamaUrl);
    logger.LogInformation("Embedding Model: {EmbeddingModel}", options.EmbeddingModel);

    var exitCode = await preprocessor.ProcessAsync(options);

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
