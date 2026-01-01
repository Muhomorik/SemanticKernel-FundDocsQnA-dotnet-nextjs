using Backend.API.Domain.ValueObjects;

namespace Backend.API.Domain.Interfaces;

/// <summary>
/// Domain interface for generating embeddings from text.
/// </summary>
public interface IEmbeddingGenerator
{
    Task<EmbeddingVector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
