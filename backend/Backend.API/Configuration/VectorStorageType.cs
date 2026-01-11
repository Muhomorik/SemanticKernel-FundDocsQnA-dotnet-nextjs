namespace Backend.API.Configuration;

/// <summary>
/// Defines the vector storage backend type.
/// </summary>
public enum VectorStorageType
{
    /// <summary>
    /// In-memory vector store using embeddings.json (default, backward compatible).
    /// Data is loaded at startup and lost on restart.
    /// </summary>
    InMemory = 0,

    /// <summary>
    /// Azure Cosmos DB vector store with persistent storage.
    /// Requires CosmosDbEndpoint, CosmosDbDatabaseName, and CosmosDbContainerName configuration.
    /// </summary>
    CosmosDb = 1
}
