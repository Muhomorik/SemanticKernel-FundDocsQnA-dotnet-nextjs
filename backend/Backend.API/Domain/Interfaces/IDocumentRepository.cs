using Backend.API.Domain.Models;

namespace Backend.API.Domain.Interfaces;

/// <summary>
/// Repository pattern for accessing document chunks.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Initializes the repository (loads data, connects to database, etc.).
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all document chunks from the repository.
    /// </summary>
    Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync();

    /// <summary>
    /// Gets the total number of chunks in the repository.
    /// </summary>
    int GetChunkCount();

    /// <summary>
    /// Gets a value indicating whether the repository has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Adds new chunks to the repository.
    /// Supported implementations: CosmosDbDocumentRepository.
    /// Throws NotSupportedException for FileBasedDocumentRepository (read-only).
    /// </summary>
    Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates existing chunks in the repository.
    /// Supported implementations: CosmosDbDocumentRepository.
    /// Throws NotSupportedException for FileBasedDocumentRepository (read-only).
    /// </summary>
    Task UpdateChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all chunks for a specific source file.
    /// Supported implementations: CosmosDbDocumentRepository.
    /// Throws NotSupportedException for FileBasedDocumentRepository (read-only).
    /// </summary>
    Task DeleteChunksBySourceAsync(string sourceFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all chunks in the repository with the provided set.
    /// Supported implementations: CosmosDbDocumentRepository.
    /// Throws NotSupportedException for FileBasedDocumentRepository (read-only).
    /// </summary>
    Task ReplaceAllChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
}
