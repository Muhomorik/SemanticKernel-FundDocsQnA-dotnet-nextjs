namespace Backend.API.Infrastructure.LLM.Configuration;

/// <summary>
/// Configuration interface for Groq provider.
/// Abstracts Groq-specific settings from infrastructure concerns.
/// </summary>
public interface IGroqConfiguration
{
    /// <summary>
    /// Gets the Groq API key for authentication.
    /// </summary>
    /// <example>Example: "gsk_abc123..."</example>
    string ApiKey { get; }

    /// <summary>
    /// Gets the Groq model name for chat completion.
    /// </summary>
    /// <example>Example: "llama-3.3-70b-versatile", "mixtral-8x7b-32768", "gemma2-9b-it"</example>
    string Model { get; }

    /// <summary>
    /// Gets the Groq API endpoint URL.
    /// Uses OpenAI-compatible API format.
    /// </summary>
    /// <example>Example: "https://api.groq.com/openai/v1"</example>
    string ApiUrl { get; }
}
