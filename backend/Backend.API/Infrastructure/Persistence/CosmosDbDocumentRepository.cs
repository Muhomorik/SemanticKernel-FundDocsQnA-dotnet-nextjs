using Microsoft.Azure.Cosmos;
using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;
using Backend.API.Infrastructure.Persistence.Models;

namespace Backend.API.Infrastructure.Persistence;

/// <summary>
/// Cosmos DB-backed document repository with vector search support.
/// Uses partition key /sourceFile for efficient per-file operations.
/// </summary>
public class CosmosDbDocumentRepository : IDocumentRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _containerName;
    private readonly ILogger<CosmosDbDocumentRepository> _logger;

    private Container? _container;
    private bool _isInitialized;
    private int _chunkCount;

    public bool IsInitialized => _isInitialized;

    public CosmosDbDocumentRepository(
        CosmosClient cosmosClient,
        Configuration.BackendOptions options,
        ILogger<CosmosDbDocumentRepository> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _databaseName = options.CosmosDbDatabaseName
            ?? throw new ArgumentNullException(nameof(options.CosmosDbDatabaseName));
        _containerName = options.CosmosDbContainerName;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Initializing Cosmos DB repository: Database={Database}, Container={Container}",
            _databaseName, _containerName);

        try
        {
            // Get database reference
            var database = _cosmosClient.GetDatabase(_databaseName);

            // Get container reference (assumes container already exists with vector index)
            _container = database.GetContainer(_containerName);

            // Verify container accessibility by reading item count
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            var iterator = _container.GetItemQueryIterator<int>(query);
            var response = await iterator.ReadNextAsync(cancellationToken);
            _chunkCount = response.FirstOrDefault();

            _logger.LogInformation("Cosmos DB repository initialized: {Count} chunks found", _chunkCount);
            _isInitialized = true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError("Database or container not found: {Database}/{Container}", _databaseName, _containerName);
            throw new InvalidOperationException(
                $"Cosmos DB container not found: {_databaseName}/{_containerName}. " +
                "Please create the container with vector indexing enabled.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Cosmos DB repository");
            throw;
        }
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync()
    {
        if (!_isInitialized || _container == null)
            throw new InvalidOperationException("Repository not initialized");

        _logger.LogDebug("Fetching all chunks from Cosmos DB");

        var query = new QueryDefinition("SELECT * FROM c");
        var iterator = _container.GetItemQueryIterator<CosmosDbDocumentDto>(query);

        var chunks = new List<DocumentChunk>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var dto in response)
            {
                chunks.Add(DocumentChunk.Create(
                    dto.Id,
                    dto.Text,
                    dto.Embedding,
                    dto.SourceFile,
                    dto.Page));
            }
        }

        _logger.LogInformation("Retrieved {Count} chunks from Cosmos DB", chunks.Count);
        return chunks;
    }

    public int GetChunkCount() => _chunkCount;

    public async Task AddChunksAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _container == null)
            throw new InvalidOperationException("Repository not initialized");

        var chunkList = chunks.ToList();
        _logger.LogInformation("Adding {Count} chunks to Cosmos DB", chunkList.Count);

        foreach (var chunk in chunkList)
        {
            var dto = new CosmosDbDocumentDto
            {
                Id = chunk.Id,
                SourceFile = chunk.Metadata.Source,
                Page = chunk.Metadata.Page,
                Text = chunk.Text,
                Embedding = chunk.Vector.Values
            };

            await _container.CreateItemAsync(
                dto,
                new PartitionKey(dto.SourceFile),
                cancellationToken: cancellationToken);
        }

        _chunkCount += chunkList.Count;
        _logger.LogInformation("Successfully added {Count} chunks", chunkList.Count);
    }

    public async Task UpdateChunksAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _container == null)
            throw new InvalidOperationException("Repository not initialized");

        var chunkList = chunks.ToList();
        _logger.LogInformation("Updating {Count} chunks in Cosmos DB", chunkList.Count);

        foreach (var chunk in chunkList)
        {
            var dto = new CosmosDbDocumentDto
            {
                Id = chunk.Id,
                SourceFile = chunk.Metadata.Source,
                Page = chunk.Metadata.Page,
                Text = chunk.Text,
                Embedding = chunk.Vector.Values
            };

            await _container.UpsertItemAsync(
                dto,
                new PartitionKey(dto.SourceFile),
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Successfully updated {Count} chunks", chunkList.Count);
    }

    public async Task DeleteChunksBySourceAsync(
        string sourceFile,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _container == null)
            throw new InvalidOperationException("Repository not initialized");

        _logger.LogInformation("Deleting all chunks for source: {Source}", sourceFile);

        // Query all items with this partition key
        var query = new QueryDefinition(
            "SELECT c.id FROM c WHERE c.sourceFile = @sourceFile")
            .WithParameter("@sourceFile", sourceFile);

        var iterator = _container.GetItemQueryIterator<dynamic>(query);
        var deletedCount = 0;

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                await _container.DeleteItemAsync<CosmosDbDocumentDto>(
                    item.id.ToString(),
                    new PartitionKey(sourceFile),
                    cancellationToken: cancellationToken);
                deletedCount++;
            }
        }

        _chunkCount -= deletedCount;
        _logger.LogInformation("Deleted {Count} chunks for source: {Source}", deletedCount, sourceFile);
    }

    public async Task ReplaceAllChunksAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _container == null)
            throw new InvalidOperationException("Repository not initialized");

        var chunkList = chunks.ToList();
        _logger.LogWarning("Replacing ALL chunks in Cosmos DB with {Count} new chunks", chunkList.Count);

        // Delete all existing items (consider using stored procedure for efficiency in production)
        var query = new QueryDefinition("SELECT c.id, c.sourceFile FROM c");
        var iterator = _container.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                await _container.DeleteItemAsync<CosmosDbDocumentDto>(
                    item.id.ToString(),
                    new PartitionKey(item.sourceFile.ToString()),
                    cancellationToken: cancellationToken);
            }
        }

        // Insert new chunks
        foreach (var chunk in chunkList)
        {
            var dto = new CosmosDbDocumentDto
            {
                Id = chunk.Id,
                SourceFile = chunk.Metadata.Source,
                Page = chunk.Metadata.Page,
                Text = chunk.Text,
                Embedding = chunk.Vector.Values
            };

            await _container.CreateItemAsync(
                dto,
                new PartitionKey(dto.SourceFile),
                cancellationToken: cancellationToken);
        }

        _chunkCount = chunkList.Count;
        _logger.LogInformation("Successfully replaced all chunks: {Count} chunks now in database", _chunkCount);
    }
}
