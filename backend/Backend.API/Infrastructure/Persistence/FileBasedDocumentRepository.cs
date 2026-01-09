using System.Text.Json;
using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;
using Backend.API.Infrastructure.Persistence.Models;

namespace Backend.API.Infrastructure.Persistence;

/// <summary>
/// Repository that loads document chunks from the embeddings.json file.
/// Maps persistence DTOs to domain models.
/// </summary>
public class FileBasedDocumentRepository : IDocumentRepository
{
    private readonly string _filePath;
    private readonly ILogger<FileBasedDocumentRepository> _logger;
    private List<DocumentChunk> _chunks = new();
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public FileBasedDocumentRepository(
        Configuration.BackendOptions options,
        ILogger<FileBasedDocumentRepository> logger)
    {
        _filePath = options.EmbeddingsFilePath;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading embeddings from {Path}", _filePath);

        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"Embeddings file not found: {_filePath}");

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        var dtos = JsonSerializer.Deserialize<List<EmbeddingRecordDto>>(json);

        if (dtos == null || dtos.Count == 0)
            throw new InvalidOperationException("No embeddings found in file");

        // Map DTOs to domain models
        _chunks = dtos.Select(dto => DocumentChunk.Create(
            dto.Id,
            dto.Text,
            dto.Embedding,
            dto.Source,
            dto.Page
        )).ToList();

        _logger.LogInformation("Loaded {Count} document chunks", _chunks.Count);
        _isInitialized = true;
    }

    public Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Repository not initialized");

        return Task.FromResult<IReadOnlyList<DocumentChunk>>(_chunks);
    }

    public int GetChunkCount() => _chunks.Count;

    public Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "AddChunksAsync is not supported for FileBasedDocumentRepository (read-only). " +
            "Use CosmosDbDocumentRepository for write operations.");
    }

    public Task UpdateChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "UpdateChunksAsync is not supported for FileBasedDocumentRepository (read-only). " +
            "Use CosmosDbDocumentRepository for write operations.");
    }

    public Task DeleteChunksBySourceAsync(string sourceFile, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "DeleteChunksBySourceAsync is not supported for FileBasedDocumentRepository (read-only). " +
            "Use CosmosDbDocumentRepository for write operations.");
    }

    public Task ReplaceAllChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "ReplaceAllChunksAsync is not supported for FileBasedDocumentRepository (read-only). " +
            "Use CosmosDbDocumentRepository for write operations.");
    }
}
