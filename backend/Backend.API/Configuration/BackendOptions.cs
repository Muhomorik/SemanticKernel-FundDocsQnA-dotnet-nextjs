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
    /// Gets or sets the Groq API key for LLM chat completion.
    /// Can be overridden by the GROQ_API_KEY environment variable.
    /// </summary>
    public required string GroqApiKey { get; init; }

    /// <summary>
    /// Gets or sets the Groq model name to use for chat completion.
    /// </summary>
    public required string GroqModel { get; init; }

    /// <summary>
    /// Gets or sets the Groq API endpoint URL.
    /// </summary>
    public required string GroqApiUrl { get; init; }

    /// <summary>
    /// Gets or sets the Ollama embedding model name.
    /// Must match the model used by the Preprocessor.
    /// </summary>
    public required string EmbeddingModel { get; init; }

    /// <summary>
    /// Gets or sets the Ollama server URL.
    /// </summary>
    public required string OllamaUrl { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of search results to return.
    /// </summary>
    public int MaxSearchResults { get; init; } = 5;

    /// <summary>
    /// Gets or sets the memory collection name.
    /// </summary>
    public required string MemoryCollectionName { get; init; }
}
