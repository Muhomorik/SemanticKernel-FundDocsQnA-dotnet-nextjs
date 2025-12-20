using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;

namespace Preprocessor.Services;

/// <summary>
/// Generates embeddings using Ollama via Semantic Kernel.
/// </summary>
/// <remarks>
/// <para><strong>Prerequisites:</strong></para>
/// <list type="bullet">
///   <item>
///     <description>
///       <strong>Ollama Server:</strong> Must be running and accessible. Download from https://ollama.com/download
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>Default Port:</strong> 11434 (http://localhost:11434). Override with --ollama-url CLI option.
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>Embedding Model:</strong> Default is 'nomic-embed-text'. Pull with: <c>ollama pull nomic-embed-text</c>
///       Override with --embedding-model CLI option.
///     </description>
///   </item>
/// </list>
/// <para><strong>CLI Options:</strong></para>
/// <list type="table">
///   <listheader>
///     <term>Option</term>
///     <description>Description</description>
///   </listheader>
///   <item>
///     <term>--ollama-url</term>
///     <description>Ollama server URL (default: http://localhost:11434)</description>
///   </item>
///   <item>
///     <term>--embedding-model</term>
///     <description>Embedding model name (default: nomic-embed-text)</description>
///   </item>
/// </list>
/// <para><strong>Common Errors:</strong></para>
/// <list type="bullet">
///   <item>
///     <description>
///       <strong>HttpRequestException:</strong> Ollama server not running or network unreachable.
///       Start Ollama and verify it's accessible at the configured URL.
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>TaskCanceledException:</strong> Request timeout. Check if Ollama is responsive or model is loaded.
///     </description>
///   </item>
/// </list>
/// </remarks>
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

    /// <summary>
    /// Tests connectivity to the Ollama server by attempting to generate a simple embedding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the server is reachable and the model is loaded, false otherwise.</returns>
    /// <remarks>
    /// This method is useful for validating the Ollama configuration before processing large batches.
    /// It generates a simple test embedding to verify connectivity and model availability.
    /// </remarks>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing connection to Ollama server...");

            // Try to generate a simple embedding as a health check
            var testEmbedding = await GenerateEmbeddingAsync("test", cancellationToken);

            if (testEmbedding.Length > 0)
            {
                _logger.LogInformation(
                    "Successfully connected to Ollama server. Embedding model is loaded and responsive (generated {Dimensions}D vector).",
                    testEmbedding.Length);
                return true;
            }

            _logger.LogWarning("Connected to Ollama server but received empty embedding");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama server during health check");
            return false;
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Cannot generate embedding for null or empty text");
            return Array.Empty<float>();
        }

        _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

        try
        {
            var embedding = await _embeddingGenerator.GenerateAsync(new[] { text }, cancellationToken: cancellationToken);
            var vector = embedding.FirstOrDefault()?.Vector.ToArray() ?? Array.Empty<float>();

            _logger.LogDebug("Generated embedding with {Dimensions} dimensions", vector.Length);

            return vector;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to generate embedding: Ollama server not reachable. " +
                "Ensure Ollama is running and accessible at the configured URL. " +
                "Check if the embedding model is pulled: ollama pull <model-name>");
            throw new InvalidOperationException(
                "Failed to connect to Ollama server. Ensure Ollama is running and the embedding model is available. " +
                "See logs for details.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex,
                "Embedding generation timed out. The Ollama server may be overloaded or the model may not be loaded. " +
                "Try: ollama run <model-name> to ensure the model is ready.");
            throw new InvalidOperationException(
                "Embedding generation timed out. The Ollama server may be slow or unresponsive. " +
                "See logs for details.", ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Embedding generation was cancelled");
            throw; // Re-throw cancellation exceptions without wrapping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error generating embedding. This may indicate an issue with the Ollama server or model configuration.");
            throw new InvalidOperationException(
                "Failed to generate embedding due to an unexpected error. See logs for details.", ex);
        }
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();

        // Input validation
        if (textList.Count == 0)
        {
            _logger.LogWarning("Cannot generate embeddings for empty text list");
            return Array.Empty<float[]>();
        }

        // Filter out null/empty texts
        var validTexts = textList.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (validTexts.Count < textList.Count)
        {
            _logger.LogWarning(
                "Filtered out {FilteredCount} null or empty texts from batch of {TotalCount}",
                textList.Count - validTexts.Count,
                textList.Count);
        }

        if (validTexts.Count == 0)
        {
            _logger.LogWarning("No valid texts remaining after filtering");
            return Array.Empty<float[]>();
        }

        _logger.LogDebug("Generating embeddings for {Count} texts", validTexts.Count);

        try
        {
            var embeddings = await _embeddingGenerator.GenerateAsync(validTexts, cancellationToken: cancellationToken);
            var vectors = embeddings.Select(e => e.Vector.ToArray()).ToList();

            _logger.LogDebug("Generated {Count} embeddings", vectors.Count);

            return vectors;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to generate embeddings for batch of {Count} texts: Ollama server not reachable. " +
                "Ensure Ollama is running and accessible at the configured URL. " +
                "Check if the embedding model is pulled: ollama pull <model-name>",
                validTexts.Count);
            throw new InvalidOperationException(
                $"Failed to connect to Ollama server while generating {validTexts.Count} embeddings. " +
                "Ensure Ollama is running and the embedding model is available. See logs for details.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex,
                "Embedding generation timed out for batch of {Count} texts. " +
                "The Ollama server may be overloaded or the model may not be loaded. " +
                "Try: ollama run <model-name> to ensure the model is ready.",
                validTexts.Count);
            throw new InvalidOperationException(
                $"Embedding generation timed out for batch of {validTexts.Count} texts. " +
                "The Ollama server may be slow or unresponsive. See logs for details.", ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Batch embedding generation was cancelled");
            throw; // Re-throw cancellation exceptions without wrapping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error generating embeddings for batch of {Count} texts. " +
                "This may indicate an issue with the Ollama server or model configuration.",
                validTexts.Count);
            throw new InvalidOperationException(
                $"Failed to generate embeddings for batch of {validTexts.Count} texts due to an unexpected error. " +
                "See logs for details.", ex);
        }
    }
}
