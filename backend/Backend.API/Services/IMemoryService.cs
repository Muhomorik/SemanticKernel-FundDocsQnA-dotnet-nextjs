using Backend.API.Models;

namespace Backend.API.Services;

/// <summary>
/// Service for loading and searching document embeddings.
/// </summary>
public interface IMemoryService
{
    /// <summary>
    /// Loads embeddings from the configured file path and initializes the service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs semantic search to find the most relevant document chunks.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of the most relevant embedding records.</returns>
    Task<List<EmbeddingRecord>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of loaded embeddings.
    /// </summary>
    /// <returns>The count of embeddings.</returns>
    int GetEmbeddingCount();

    /// <summary>
    /// Gets a value indicating whether the service has been initialized.
    /// </summary>
    bool IsInitialized { get; }
}
