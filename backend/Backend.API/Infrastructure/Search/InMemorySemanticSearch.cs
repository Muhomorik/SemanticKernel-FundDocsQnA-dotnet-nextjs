using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;
using Backend.API.Infrastructure.Search.Mapping;
using Backend.API.Infrastructure.Search.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Backend.API.Infrastructure.Search;

/// <summary>
/// Semantic search implementation using Semantic Kernel's InMemoryVectorStore.
/// Provides vector similarity search with built-in cosine similarity calculation.
/// </summary>
public class InMemorySemanticSearch : ISemanticSearch
{
    private readonly IDocumentRepository _repository;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<InMemorySemanticSearch> _logger;

    private readonly InMemoryVectorStore _vectorStore;
    private VectorStoreCollection<string, DocumentChunkRecord>? _collection;
    private bool _isInitialized;

    private const string CollectionName = "documents";

    public InMemorySemanticSearch(
        IDocumentRepository repository,
        IEmbeddingGenerator embeddingGenerator,
        ILogger<InMemorySemanticSearch> logger)
    {
        _repository = repository;
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
        _vectorStore = new InMemoryVectorStore();
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching for: {Query} (max: {Max})", query, maxResults);

        // Lazy initialization of VectorStore collection
        await EnsureInitializedAsync(cancellationToken);

        // Generate query embedding
        var queryVector = await _embeddingGenerator.GenerateEmbeddingAsync(
            query,
            cancellationToken);

        // Perform vector search using VectorStore
        var searchVector = new ReadOnlyMemory<float>(queryVector.Values);
        var searchResults = _collection!.SearchAsync(searchVector, top: maxResults, cancellationToken: cancellationToken);

        // Convert to domain SearchResults with normalized scores
        var results = new List<SearchResult>();
        await foreach (var result in searchResults.WithCancellation(cancellationToken))
        {
            var chunk = DocumentChunkMapper.ToDomain(result.Record);

            // Normalize score from cosine similarity [-1, 1] to [0, 1]
            // CosineSimilarity: 1 = identical vectors, -1 = opposite vectors
            var score = result.Score ?? 0.0;
            var normalizedScore = (float)((score + 1.0) / 2.0);

            results.Add(new SearchResult(chunk, normalizedScore));
        }

        _logger.LogInformation("Search returned {Count} results", results.Count);
        return results;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized) return;

        _logger.LogInformation("Initializing VectorStore collection");

        _collection = _vectorStore.GetCollection<string, DocumentChunkRecord>(CollectionName);
        await _collection.EnsureCollectionExistsAsync(cancellationToken);

        // Load chunks from repository and upsert into VectorStore
        var chunks = await _repository.GetAllChunksAsync();
        foreach (var chunk in chunks)
        {
            var record = DocumentChunkMapper.ToRecord(chunk);
            await _collection.UpsertAsync(record, cancellationToken: cancellationToken);
        }

        _logger.LogInformation("VectorStore initialized with {Count} records", chunks.Count);
        _isInitialized = true;
    }
}
