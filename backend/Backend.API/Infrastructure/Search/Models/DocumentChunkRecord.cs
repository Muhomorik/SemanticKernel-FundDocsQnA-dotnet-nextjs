using Microsoft.Extensions.VectorData;

namespace Backend.API.Infrastructure.Search.Models;

/// <summary>
/// Infrastructure record for VectorStore operations.
/// Contains SK-specific attributes without polluting domain model.
/// </summary>
public sealed class DocumentChunkRecord
{
    [VectorStoreKey]
    public string Id { get; init; } = string.Empty;

    [VectorStoreData]
    public string Text { get; init; } = string.Empty;

    [VectorStoreData]
    public string Source { get; init; } = string.Empty;

    [VectorStoreData]
    public int Page { get; init; }

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; init; }
}
