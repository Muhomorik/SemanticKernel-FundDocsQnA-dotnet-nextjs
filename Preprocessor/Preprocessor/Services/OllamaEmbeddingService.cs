using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;

namespace Preprocessor.Services;

/// <summary>
/// Generates embeddings using Ollama via Semantic Kernel.
/// </summary>
public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(
        ITextEmbeddingGenerationService embeddingService,
        ILogger<OllamaEmbeddingService> logger)
    {
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

        var result = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);

        _logger.LogDebug("Generated embedding with {Dimensions} dimensions", result.Length);

        return result.ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        _logger.LogDebug("Generating embeddings for {Count} texts", textList.Count);

        var results = await _embeddingService.GenerateEmbeddingsAsync(textList, cancellationToken: cancellationToken);

        _logger.LogDebug("Generated {Count} embeddings", results.Count);

        return results.Select(r => r.ToArray()).ToList();
    }
}
