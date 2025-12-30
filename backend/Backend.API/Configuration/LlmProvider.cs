namespace Backend.API.Configuration;

/// <summary>
/// Supported LLM providers for chat completion.
/// </summary>
public enum LlmProvider
{
    /// <summary>
    /// OpenAI provider (gpt-4o-mini, default).
    /// </summary>
    OpenAI,

    /// <summary>
    /// Groq provider (llama-3.3-70b-versatile, optional free tier).
    /// </summary>
    Groq
}

/// <summary>
/// Extension methods for LlmProvider enum.
/// </summary>
public static class LlmProviderExtensions
{
    /// <summary>
    /// Parses a string to LlmProvider enum, case-insensitive.
    /// </summary>
    /// <param name="value">The string value to parse (e.g., "openai", "OpenAI", "groq", "Groq")</param>
    /// <returns>The parsed LlmProvider enum value</returns>
    /// <exception cref="ArgumentException">Thrown when the value is invalid</exception>
    /// <example>
    /// <code>
    /// var provider = LlmProviderExtensions.Parse("OpenAI"); // Returns LlmProvider.OpenAI
    /// var provider2 = LlmProviderExtensions.Parse("groq");  // Returns LlmProvider.Groq
    /// </code>
    /// </example>
    public static LlmProvider Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("LLM provider value cannot be null or empty", nameof(value));
        }

        if (Enum.TryParse<LlmProvider>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new ArgumentException(
            $"Invalid LlmProvider: '{value}'. Valid values are: {string.Join(", ", Enum.GetNames<LlmProvider>())}",
            nameof(value));
    }

    /// <summary>
    /// Tries to parse a string to LlmProvider enum, case-insensitive.
    /// </summary>
    /// <param name="value">The string value to parse</param>
    /// <param name="result">The parsed LlmProvider if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryParse(string? value, out LlmProvider result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Enum.TryParse(value, ignoreCase: true, out result);
    }
}
