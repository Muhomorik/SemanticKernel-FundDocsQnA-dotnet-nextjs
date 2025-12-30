using Backend.API.Domain.Models;

namespace Backend.API.Domain.Interfaces;

/// <summary>
/// Repository pattern for accessing document chunks.
/// </summary>
public interface IDocumentRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync();
    int GetChunkCount();
    bool IsInitialized { get; }
}
