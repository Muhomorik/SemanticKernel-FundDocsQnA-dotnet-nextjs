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
    /// Creates ApplicationOptions with default system prompt.
    /// </summary>
    /// <param name="maxSearchResults">Maximum number of search results (default: 10)</param>
    /// <param name="systemPrompt">Custom system prompt (optional, uses default if not provided)</param>
    /// <returns>Configured ApplicationOptions instance</returns>
    public static ApplicationOptions Create(
        int maxSearchResults = 10,
        string? systemPrompt = null)
    {
        return new ApplicationOptions
        {
            MaxSearchResults = maxSearchResults,
            SystemPrompt = systemPrompt ?? GetDefaultSystemPrompt()
        };
    }

    /// <summary>
    /// Gets the default system prompt for financial document Q&amp;A.
    /// </summary>
    /// <returns>Default system prompt string</returns>
    public static string GetDefaultSystemPrompt() =>
        @"You are a helpful assistant that answers questions about financial fund documents.

Use the following context to answer the question. If the answer is not in the context, say ""I don't have enough information to answer this question.""

Always base your answer strictly on the provided context. Do not make up information.";
}
