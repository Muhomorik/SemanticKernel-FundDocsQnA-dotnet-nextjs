namespace Backend.API.Configuration;

/// <summary>
/// Configuration options for the backend API.
/// </summary>
public record BackendOptions
{
    /// <summary>
    /// Gets or sets the path to the embeddings JSON file.
    /// Can be overridden by the EMBEDDINGS_PATH environment variable.
    /// </summary>
    public required string EmbeddingsFilePath { get; init; }

    /// <summary>
    /// Gets or sets the LLM provider to use for chat completion.
    /// Can be overridden by the LLM_PROVIDER environment variable.
    /// </summary>
    public LlmProvider LlmProvider { get; init; } = LlmProvider.OpenAI;

    /// <summary>
    /// Gets or sets the OpenAI API key for embedding generation and chat completion.
    /// Can be overridden by the OPENAI_API_KEY environment variable.
    /// </summary>
    public required string OpenAIApiKey { get; init; }

    /// <summary>
    /// Gets or sets the OpenAI embedding model name.
    /// Must match the model used by the Preprocessor for vector space compatibility.
    /// </summary>
    public required string OpenAIEmbeddingModel { get; init; }

    /// <summary>
    /// Gets or sets the OpenAI chat model name.
    /// Only used when LlmProvider is set to LlmProvider.OpenAI.
    /// </summary>
    public string OpenAIChatModel { get; init; } = "gpt-4o-mini";

    /// <summary>
    /// Gets or sets the Groq API key for LLM chat completion.
    /// Only required when LlmProvider is set to LlmProvider.Groq.
    /// Can be overridden by the GROQ_API_KEY environment variable.
    /// </summary>
    public string? GroqApiKey { get; init; }

    /// <summary>
    /// Gets or sets the Groq model name to use for chat completion.
    /// Only used when LlmProvider is set to LlmProvider.Groq.
    /// </summary>
    public string? GroqModel { get; init; }

    /// <summary>
    /// Gets or sets the Groq API endpoint URL.
    /// Only used when LlmProvider is set to LlmProvider.Groq.
    /// </summary>
    public string? GroqApiUrl { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of search results to return.
    /// </summary>
    public int MaxSearchResults { get; init; } = 10;

    /// <summary>
    /// Gets or sets the memory collection name.
    /// </summary>
    public required string MemoryCollectionName { get; init; }

    /// <summary>
    /// Gets or sets the vector storage backend type.
    /// Default is InMemory (backward compatible).
    /// Can be overridden by the VECTOR_STORAGE_TYPE environment variable or BackendOptions__VectorStorageType.
    /// </summary>
    public VectorStorageType VectorStorageType { get; init; } = VectorStorageType.InMemory;

    /// <summary>
    /// Gets or sets the Cosmos DB endpoint URL.
    /// Required when VectorStorageType is CosmosDb.
    /// Example: https://your-cosmos-account.documents.azure.com:443/
    /// Can be overridden by BackendOptions__CosmosDbEndpoint environment variable.
    /// </summary>
    public string? CosmosDbEndpoint { get; init; }

    /// <summary>
    /// Gets or sets the Cosmos DB database name.
    /// Required when VectorStorageType is CosmosDb.
    /// Can be overridden by BackendOptions__CosmosDbDatabaseName environment variable.
    /// </summary>
    public string? CosmosDbDatabaseName { get; init; }

    /// <summary>
    /// Gets or sets the Cosmos DB container name.
    /// Only used when VectorStorageType is CosmosDb.
    /// Default: embeddings
    /// Can be overridden by BackendOptions__CosmosDbContainerName environment variable.
    /// </summary>
    public string CosmosDbContainerName { get; init; } = "embeddings";

    /// <summary>
    /// Gets or sets the API key for authenticating Preprocessor requests to embedding endpoints.
    /// Required when VectorStorageType is CosmosDb.
    /// Should be stored in Azure Key Vault in production.
    /// Can be overridden by BackendOptions__EmbeddingApiKey environment variable.
    /// </summary>
    public string? EmbeddingApiKey { get; init; }

    /// <summary>
    /// Gets or sets the allowed CORS origins.
    /// Can be overridden via Azure App Service Configuration using BackendOptions__AllowedOrigins__0, etc.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = ["http://localhost:3000", "http://localhost:3001"];

    /// <summary>
    /// Gets or sets an optional custom system prompt for the LLM.
    /// If not set, uses the default hardened prompt from SystemPromptFactory.
    /// Can be set via environment variable: BackendOptions:SystemPrompt or BackendOptions__SystemPrompt
    /// </summary>
    public string? SystemPrompt { get; init; }
}