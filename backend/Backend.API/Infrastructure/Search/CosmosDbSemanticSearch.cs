using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;
using Microsoft.Azure.Cosmos;

namespace Backend.API.Infrastructure.Search;

/// <summary>
/// Semantic search implementation using Cosmos DB vector search.
/// Leverages native vector indexing for efficient similarity search.
/// </summary>
public class CosmosDbSemanticSearch : ISemanticSearch
{
    private readonly IDocumentRepository _repository;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _containerName;
    private readonly ILogger<CosmosDbSemanticSearch> _logger;

    private Container? _container;

    public CosmosDbSemanticSearch(
        IDocumentRepository repository,
        IEmbeddingGenerator embeddingGenerator,
        CosmosClient cosmosClient,
        Configuration.BackendOptions options,
        ILogger<CosmosDbSemanticSearch> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _databaseName = options.CosmosDbDatabaseName
            ?? throw new ArgumentNullException(nameof(options.CosmosDbDatabaseName));
        _containerName = options.CosmosDbContainerName;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching Cosmos DB for: {Query} (max: {Max})", query, maxResults);

        // Lazy initialization
        await EnsureInitializedAsync(cancellationToken);

        // Generate query embedding
        var queryVector = await _embeddingGenerator.GenerateEmbeddingAsync(
            query,
            cancellationToken);

        // Perform vector search using Cosmos DB native vector search
        // VectorDistance returns cosine distance (0 = identical, 2 = opposite)
        var vectorSearchQuery = new QueryDefinition(
            @"SELECT TOP @maxResults c.id, c.sourceFile, c.page, c.text, c.embedding,
              VectorDistance(c.embedding, @queryEmbedding) AS distance
              FROM c
              ORDER BY VectorDistance(c.embedding, @queryEmbedding)")
            .WithParameter("@maxResults", maxResults)
            .WithParameter("@queryEmbedding", queryVector.Values);

        var iterator = _container!.GetItemQueryIterator<dynamic>(vectorSearchQuery);
        var results = new List<SearchResult>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                // Reconstruct DocumentChunk from Cosmos DB item
                var chunk = DocumentChunk.Create(
                    item.id.ToString(),
                    item.text.ToString(),
                    ((Newtonsoft.Json.Linq.JArray)item.embedding).ToObject<float[]>()!,
                    item.sourceFile.ToString(),
                    (int)item.page);

                // Cosmos DB returns cosine distance (0 = identical, 2 = opposite)
                // Convert to similarity score (1 = identical, 0 = orthogonal)
                var distance = (double)item.distance;
                var similarityScore = (float)(1.0 - distance / 2.0);

                results.Add(new SearchResult(chunk, similarityScore));
            }
        }

        _logger.LogInformation("Cosmos DB search returned {Count} results", results.Count);
        return results;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_container != null) return;

        await _repository.InitializeAsync(cancellationToken);

        var database = _cosmosClient.GetDatabase(_databaseName);
        _container = database.GetContainer(_containerName);

        _logger.LogInformation("Cosmos DB semantic search initialized");
    }
}
