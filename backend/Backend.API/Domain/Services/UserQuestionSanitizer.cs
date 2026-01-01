using Backend.API.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace Backend.API.Domain.Services;

/// <summary>
/// Pure domain service for sanitizing user question input to prevent prompt injection.
/// No I/O dependencies - pure string transformation logic.
/// </summary>
/// <remarks>
/// Security Strategy:
/// 1. Remove control characters (null bytes, tabs, excessive newlines)
/// 2. Normalize whitespace (collapse multiple spaces, trim)
/// 3. Preserve legitimate use cases (punctuation, basic formatting)
/// 4. Avoid over-aggressive filtering that breaks user experience
/// </remarks>
public partial class UserQuestionSanitizer : IUserQuestionSanitizer
{
    // Regex patterns compiled for performance
    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.Compiled)]
    private static partial Regex ControlCharactersRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultipleWhitespaceRegex();

    [GeneratedRegex(@"[\r\n]{3,}", RegexOptions.Compiled)]
    private static partial Regex ExcessiveNewlinesRegex();

    /// <summary>
    /// Sanitizes user question input to prevent prompt injection attacks.
    /// </summary>
    public string Sanitize(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return string.Empty;

        var sanitized = question;

        // Step 1: Remove null bytes and control characters (except \n, \r, \t)
        // Keep newlines and tabs for legitimate formatting
        sanitized = ControlCharactersRegex().Replace(sanitized, string.Empty);

        // Step 2: Normalize excessive newlines (more than 2 consecutive)
        // Allow up to 2 newlines for paragraph breaks
        sanitized = ExcessiveNewlinesRegex().Replace(sanitized, "\n\n");

        // Step 3: Normalize whitespace (collapse multiple spaces)
        // But preserve single newlines
        var lines = sanitized.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = MultipleWhitespaceRegex().Replace(lines[i], " ").Trim();
        }
        sanitized = string.Join('\n', lines);

        // Step 4: Final trim
        return sanitized.Trim();
    }
}
