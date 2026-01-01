namespace Backend.API.Infrastructure.LLM.Configuration;

/// <summary>
/// Configuration interface for OpenAI provider.
/// Abstracts OpenAI-specific settings from infrastructure concerns.
/// </summary>
public interface IOpenAiConfiguration
{
    /// <summary>
    /// Gets the OpenAI API key for authentication.
    /// </summary>
    /// <example>Example: "sk-proj-abc123..."</example>
    string ApiKey { get; }

    /// <summary>
    /// Gets the OpenAI chat completion model name.
    /// </summary>
    /// <example>Example: "gpt-4o-mini", "gpt-4o", "gpt-3.5-turbo"</example>
    string ChatModel { get; }

    /// <summary>
    /// Gets the OpenAI embedding model name.
    /// Must match the model used by the Preprocessor for vector space compatibility.
    /// </summary>
    /// <example>Example: "text-embedding-3-small", "text-embedding-3-large", "text-embedding-ada-002"</example>
    string EmbeddingModel { get; }
}
