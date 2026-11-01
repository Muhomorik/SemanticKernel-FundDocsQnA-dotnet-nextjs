using System.Globalization;
using System.Text;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Deterministic conversion of raw country/sector display names (e.g. "Hälsovård", "Storbritannien")
/// into safe CSV column suffixes (e.g. "halsovard", "storbritannien") for the wide-format
/// allocations export. ASCII-only output keeps pandas attribute access (<c>df.country_usa</c>) working.
/// </summary>
internal static class AllocationColumnSanitizer
{
    /// <summary>
    /// Sanitizes a raw display name into a column suffix:
    /// Unicode FormD strips diacritics (å→a, ö→o, é→e); the result is lowercased; non-alphanumeric
    /// runs collapse to a single underscore; leading/trailing underscores are trimmed. Throws when
    /// the input is null/blank or sanitizes to an empty string (degenerate name with no
    /// representable characters).
    /// </summary>
    public static string Sanitize(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new InvalidOperationException("Cannot sanitize a null or blank display name.");

        var folded = StripDiacritics(displayName).ToLowerInvariant();

        var result = new StringBuilder(folded.Length);
        var prevWasUnderscore = false;

        foreach (var c in folded)
        {
            if (IsAsciiAlphanumeric(c))
            {
                result.Append(c);
                prevWasUnderscore = false;
            }
            else if (!prevWasUnderscore && result.Length > 0)
            {
                result.Append('_');
                prevWasUnderscore = true;
            }
        }

        var sanitized = result.ToString().TrimEnd('_');

        if (sanitized.Length == 0)
            throw new InvalidOperationException(
                $"Cannot sanitize display name '{displayName}' to a valid column suffix — no representable characters.");

        return sanitized;
    }

    private static string StripDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsAsciiAlphanumeric(char c) =>
        (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
}
