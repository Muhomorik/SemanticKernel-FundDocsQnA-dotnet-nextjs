using Backend.API.Configuration;

namespace Backend.API.ApplicationCore.Configuration;

/// <summary>
/// Application-level configuration.
/// Extracted from BackendOptions to decouple application from infrastructure concerns.
/// </summary>
public record ApplicationOptions
{
    /// <summary>
    /// Gets the maximum number of search results to return for semantic search.
    /// </summary>
    public int MaxSearchResults { get; init; } = 10;

    /// <summary>
    /// Gets the system prompt for the LLM.
    /// Instructs the LLM how to behave when answering questions.
    /// </summary>
    /// <example>
    /// Example: "You are a helpful assistant that answers questions about financial documents..."
    /// </example>
    public required string SystemPrompt { get; init; }

    /// <summary>
    /// Creates ApplicationOptions from BackendOptions using SystemPromptFactory.
    /// Uses environment-based system prompt if configured, otherwise uses hardened default.
    /// </summary>
    /// <param name="backendOptions">Backend configuration options</param>
    /// <returns>Configured ApplicationOptions instance</returns>
    public static ApplicationOptions Create(BackendOptions backendOptions)
    {
        return new ApplicationOptions
        {
            MaxSearchResults = backendOptions.MaxSearchResults,
            SystemPrompt = SystemPromptFactory.Create(backendOptions)
        };
    }

    /// <summary>
    /// Creates ApplicationOptions with custom parameters (used primarily for testing).
    /// </summary>
    /// <param name="maxSearchResults">Maximum number of search results (default: 10)</param>
    /// <param name="systemPrompt">Custom system prompt (optional, uses factory default if not provided)</param>
    /// <returns>Configured ApplicationOptions instance</returns>
    public static ApplicationOptions Create(
        int maxSearchResults = 10,
        string? systemPrompt = null)
    {
        return new ApplicationOptions
        {
            MaxSearchResults = maxSearchResults,
            SystemPrompt = systemPrompt ?? SystemPromptFactory.Create(new BackendOptions
            {
                EmbeddingsFilePath = "",
                OpenAIApiKey = "",
                OpenAIEmbeddingModel = "",
                MemoryCollectionName = ""
            })
        };
    }
}
