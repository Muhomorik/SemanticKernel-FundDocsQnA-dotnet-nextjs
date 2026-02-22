using System.Diagnostics;
using System.Text.RegularExpressions;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Strongly-typed fund identifier using ISIN (International Securities Identification Number).
/// </summary>
/// <remarks>
/// ISIN is a globally unique 12-character alphanumeric code:
/// - 2-letter country code (e.g., "SE" for Sweden, "LU" for Luxembourg)
/// - 9-character alphanumeric identifier
/// - 1-digit checksum
///
/// Examples: "SE0008613939", "LU0274208692"
///
/// Using ISIN enables multi-source crawling across different fund providers
/// without identifier conflicts.
/// </remarks>
[DebuggerDisplay("ISIN: {Isin}")]
public readonly record struct IsinId(string Isin)
{
    private static readonly Regex IsinPattern = new(@"^[A-Z]{2}[A-Z0-9]{9}[0-9]$", RegexOptions.Compiled);

    /// <summary>
    /// Creates a new <see cref="IsinId"/> with ISIN validation.
    /// </summary>
    /// <param name="isin">The ISIN code (12 characters: 2 letters + 9 alphanumeric + 1 digit).</param>
    /// <returns>A validated <see cref="IsinId"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if ISIN format is invalid.</exception>
    public static IsinId Create(string isin)
    {
        if (string.IsNullOrWhiteSpace(isin))
        {
            throw new ArgumentException("ISIN cannot be null or empty.", nameof(isin));
        }

        if (!IsinPattern.IsMatch(isin))
        {
            throw new ArgumentException(
                $"Invalid ISIN format: '{isin}'. Expected format: 2 uppercase letters + 9 alphanumeric + 1 digit (e.g., 'SE0008613939').",
                nameof(isin));
        }

        return new IsinId(isin);
    }

    /// <summary>
    /// Parses a string into a <see cref="IsinId"/> with validation.
    /// </summary>
    /// <param name="value">The ISIN string to parse.</param>
    /// <returns>A validated <see cref="IsinId"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if ISIN format is invalid.</exception>
    public static IsinId Parse(string value) => Create(value);
}
