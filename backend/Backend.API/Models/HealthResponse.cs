namespace Backend.API.Models;

/// <summary>
/// Response model for the /api/health endpoint.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Gets or sets the health status ("Healthy" or "Initializing").
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether embeddings have been loaded.
    /// </summary>
    public required bool EmbeddingsLoaded { get; init; }

    /// <summary>
    /// Gets or sets the total number of embeddings loaded.
    /// </summary>
    public required int EmbeddingCount { get; init; }
}
