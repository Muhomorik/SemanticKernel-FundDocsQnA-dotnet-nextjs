using Preprocessor.Models;

namespace Preprocessor.Outputs;

/// <summary>
/// Represents an output destination for embeddings.
/// </summary>
/// <remarks>
/// Implementations support different output strategies:
/// - <see cref="JsonEmbeddingOutput"/>: Write to local JSON file (embeddings.json)
/// - <see cref="CosmosDbEmbeddingOutput"/>: Upload to backend API (Cosmos DB)
/// </remarks>
public interface IEmbeddingOutput
{
    /// <summary>
    /// Loads existing embeddings from the output destination.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of existing embedding results, or empty list if none exist.</returns>
    Task<IReadOnlyList<EmbeddingResult>> LoadExistingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves embeddings to the output destination.
    /// </summary>
    /// <param name="embeddings">Embeddings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(IReadOnlyList<EmbeddingResult> embeddings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a display name for this output destination (for logging).
    /// </summary>
    string DisplayName { get; }
}
