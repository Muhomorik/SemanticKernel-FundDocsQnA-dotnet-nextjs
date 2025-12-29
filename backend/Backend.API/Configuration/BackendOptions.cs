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
    /// Valid values: "OpenAI" or "Groq"
    /// Can be overridden by the LLM_PROVIDER environment variable.
    /// </summary>
    public string LlmProvider { get; init; } = "OpenAI";

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
    /// Only used when LlmProvider is set to "OpenAI".
    /// </summary>
    public string OpenAIChatModel { get; init; } = "gpt-4o-mini";

    /// <summary>
    /// Gets or sets the Groq API key for LLM chat completion.
    /// Only required when LlmProvider is set to "Groq".
    /// Can be overridden by the GROQ_API_KEY environment variable.
    /// </summary>
    public string? GroqApiKey { get; init; }

    /// <summary>
    /// Gets or sets the Groq model name to use for chat completion.
    /// Only used when LlmProvider is set to "Groq".
    /// </summary>
    public string? GroqModel { get; init; }

    /// <summary>
    /// Gets or sets the Groq API endpoint URL.
    /// Only used when LlmProvider is set to "Groq".
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
    /// Gets or sets the allowed CORS origins.
    /// Can be overridden via Azure App Service Configuration using BackendOptions__AllowedOrigins__0, etc.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = ["http://localhost:3000", "http://localhost:3001"];
}