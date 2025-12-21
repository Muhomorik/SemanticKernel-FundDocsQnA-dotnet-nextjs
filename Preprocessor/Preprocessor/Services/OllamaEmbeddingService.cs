using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;

namespace Preprocessor.Services;

/// <summary>
/// Generates embeddings using Ollama or LM Studio via Semantic Kernel.
/// </summary>
/// <remarks>
/// <para>
/// Despite the class name, this service supports both Ollama and LM Studio providers.
/// It's provider-agnostic and works with any IEmbeddingGenerator implementation registered in the DI container.
/// </para>
/// <para><strong>Prerequisites by Provider:</strong></para>
/// <para><strong>Ollama:</strong></para>
/// <list type="bullet">
///   <item>
///     <description>
///       Download from https://ollama.com/download and install
///     </description>
///   </item>
///   <item>
///     <description>
///       Default URL: http://localhost:11434 (uses native <c>/api/embed</c> endpoint)
///     </description>
///   </item>
///   <item>
///     <description>
///       Pull embedding model: <c>ollama pull nomic-embed-text</c>
///     </description>
///   </item>
///   <item>
///     <description>
///       Use with: <c>--provider ollama</c>
///     </description>
///   </item>
/// </list>
/// <para><strong>LM Studio:</strong></para>
/// <list type="bullet">
///   <item>
///     <description>
///       Download from https://lmstudio.ai and install
///     </description>
///   </item>
///   <item>
///     <description>
///       Default URL: http://localhost:1234 (uses OpenAI-compatible <c>/v1/embeddings</c> endpoint)
///     </description>
///   </item>
///   <item>
///     <description>
///       Load embedding model in LM Studio's Embedding section (e.g., nomic-embed-text-v1.5-GGUF)
///     </description>
///   </item>
///   <item>
///     <description>
///       Start local server (Developer tab → Start Server)
///     </description>
///   </item>
///   <item>
///     <description>
///       Use with: <c>--provider lmstudio</c> (default)
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
///     <term>--provider</term>
///     <description>Embedding provider: 'ollama' or 'lmstudio' (default: lmstudio)</description>
///   </item>
///   <item>
///     <term>--ollama-url</term>
///     <description>Provider endpoint URL (auto-detects based on --provider if not specified)</description>
///   </item>
///   <item>
///     <term>--embedding-model</term>
///     <description>Embedding model name (default: nomic-embed-text)</description>
///   </item>
/// </list>
/// <para><strong>Common Errors and Troubleshooting:</strong></para>
/// <list type="bullet">
///   <item>
///     <description>
///       <strong>HttpRequestException:</strong> Provider server not running or network unreachable.
///       For Ollama: Verify with <c>ollama list</c>.
///       For LM Studio: Check if server is started and model is loaded.
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>TaskCanceledException:</strong> Request timeout. The provider may be slow or the model may not be loaded.
///       For Ollama: Try <c>ollama run nomic-embed-text</c> to ensure the model is ready.
///       For LM Studio: Verify the model is loaded in the Embedding section.
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>Empty Embeddings:</strong> Wrong provider selected or endpoint mismatch.
///       Verify <c>--provider</c> matches your running service (ollama vs lmstudio).
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>404 Not Found:</strong> Endpoint mismatch between provider and configured URL.
///       Ollama uses <c>/api/embed</c>, LM Studio uses <c>/v1/embeddings</c>.
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
    /// Tests connectivity to the embedding provider by attempting to generate a simple embedding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider is reachable and the model is loaded, false otherwise.</returns>
    /// <remarks>
    /// This method is useful for validating the provider configuration before processing large batches.
    /// It generates a simple test embedding to verify connectivity and model availability.
    /// Works with both Ollama and LM Studio providers.
    /// </remarks>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing connection to embedding provider...");

            // Try to generate a simple embedding as a health check
            var testEmbedding = await GenerateEmbeddingAsync("test", cancellationToken);

            if (testEmbedding.Length > 0)
            {
                _logger.LogInformation(
                    "Successfully connected to embedding provider. Model is loaded and responsive (generated {Dimensions}D vector).",
                    testEmbedding.Length);
                return true;
            }

            _logger.LogWarning("Connected to embedding provider but received empty embedding");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to embedding provider during health check");
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
            var embedding =
                await _embeddingGenerator.GenerateAsync(new[] { text }, cancellationToken: cancellationToken);
            var vector = embedding.FirstOrDefault()?.Vector.ToArray() ?? Array.Empty<float>();

            _logger.LogDebug("Generated embedding with {Dimensions} dimensions", vector.Length);

            return vector;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to generate embedding: Provider server not reachable. " +
                "Ensure your embedding provider (Ollama or LM Studio) is running and accessible at the configured URL. " +
                "For Ollama: Check 'ollama list'. " +
                "For LM Studio: Verify model loaded in Embedding section.");
            throw new InvalidOperationException(
                "Failed to connect to embedding provider. Ensure the provider is running and the embedding model is available. " +
                "See logs for details.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex,
                "Embedding generation timed out. The provider server may be overloaded or the model may not be loaded. " +
                "For Ollama: Try 'ollama run <model-name>' to ensure the model is ready. " +
                "For LM Studio: Verify the model is loaded in the Embedding section.");
            throw new InvalidOperationException(
                "Embedding generation timed out. The provider server may be slow or unresponsive. " +
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

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
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
                "Failed to generate embeddings for batch of {Count} texts: Provider server not reachable. " +
                "Ensure your embedding provider (Ollama or LM Studio) is running and accessible at the configured URL. " +
                "For Ollama: Check 'ollama list'. " +
                "For LM Studio: Verify model loaded in Embedding section.",
                validTexts.Count);
            throw new InvalidOperationException(
                $"Failed to connect to embedding provider while generating {validTexts.Count} embeddings. " +
                "Ensure the provider is running and the embedding model is available. See logs for details.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex,
                "Embedding generation timed out for batch of {Count} texts. " +
                "The provider server may be overloaded or the model may not be loaded. " +
                "For Ollama: Try 'ollama run <model-name>' to ensure the model is ready. " +
                "For LM Studio: Verify the model is loaded in the Embedding section.",
                validTexts.Count);
            throw new InvalidOperationException(
                $"Embedding generation timed out for batch of {validTexts.Count} texts. " +
                "The provider server may be slow or unresponsive. See logs for details.", ex);
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