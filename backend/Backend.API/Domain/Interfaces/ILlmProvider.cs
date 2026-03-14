namespace Backend.API.Domain.Interfaces;

/// <summary>
/// Core domain interface for LLM operations.
/// Abstracts away specific LLM provider implementations (OpenAI, Groq, etc.).
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Generates a chat completion response.
    /// </summary>
    Task<string> GenerateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a chat completion response token-by-token.
    /// </summary>
    IAsyncEnumerable<string> StreamChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider name (for logging/diagnostics).
    /// </summary>
    string ProviderName { get; }
}
