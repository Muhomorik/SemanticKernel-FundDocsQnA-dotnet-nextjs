using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;

namespace Preprocessor.Services;

/// <summary>
/// Generates embeddings using Ollama via Semantic Kernel.
/// </summary>
public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<OllamaEmbeddingService> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

        var embedding = await _embeddingGenerator.GenerateAsync(new[] { text }, cancellationToken: cancellationToken);
        var vector = embedding.FirstOrDefault()?.Vector.ToArray() ?? Array.Empty<float>();

        _logger.LogDebug("Generated embedding with {Dimensions} dimensions", vector.Length);

        return vector;
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        _logger.LogDebug("Generating embeddings for {Count} texts", textList.Count);

        var embeddings = await _embeddingGenerator.GenerateAsync(textList, cancellationToken: cancellationToken);
        var vectors = embeddings.Select(e => e.Vector.ToArray()).ToList();

        _logger.LogDebug("Generated {Count} embeddings", vectors.Count);

        return vectors;
    }
}
