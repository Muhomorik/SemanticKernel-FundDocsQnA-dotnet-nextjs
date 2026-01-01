namespace Backend.API.Domain.Interfaces;

/// <summary>
/// Domain interface for sanitizing user question input to prevent prompt injection attacks.
/// Pure abstraction with no external dependencies.
/// </summary>
public interface IUserQuestionSanitizer
{
    /// <summary>
    /// Sanitizes user question input by removing control characters, normalizing whitespace,
    /// and applying security filters.
    /// </summary>
    /// <param name="question">Raw user question to sanitize</param>
    /// <returns>Sanitized string safe for LLM prompt interpolation</returns>
    string Sanitize(string question);
}
