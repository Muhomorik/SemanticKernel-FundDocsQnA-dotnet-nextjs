namespace Backend.API.Domain.FundData.ValueObjects;

/// <summary>
/// Maps free-form Swedish fund category strings to macro-group names for Sankey chart aggregation.
/// </summary>
/// <remarks>
/// Category values come from <see cref="Models.FundProfile.Category"/> and are free-form Swedish strings.
/// Matching uses <see cref="StringComparison.OrdinalIgnoreCase"/> with <c>Contains()</c>.
/// Unmapped or null categories resolve to "Other".
/// </remarks>
public static class CategoryMacroGroup
{
    private static readonly (string Pattern, string Group)[] Mappings =
    [
        // Bonds — check specific patterns before broad ones
        ("Rante - SEK", "SEK Bonds"),
        ("Rante - euro", "Euro Bonds"),

        // Regions
        ("Sverige", "Sverige"),
        ("USA", "USA"),
        ("Europa", "Europa"),
        ("Finland", "Europa"),
        ("Global", "Global"),

        // Emerging & Asia
        ("Tillvaxtmarknader", "Emerging Markets"),
        ("Indien", "Emerging Markets"),
        ("Kina", "Emerging Markets"),
        ("Japan", "Japan/Asia"),
        ("Asien", "Japan/Asia"),

        // Other types
        ("Branschfond", "Sector Funds"),
        ("Blandfond", "Mixed Funds"),
    ];

    /// <summary>
    /// Resolves a fund category string to its macro-group name.
    /// </summary>
    /// <param name="category">Free-form Swedish category string from FundProfile.Category.</param>
    /// <returns>Macro-group name (e.g., "Sverige", "Global", "SEK Bonds"), or "Other" if unmapped.</returns>
    public static string Resolve(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return "Other";

        foreach (var (pattern, group) in Mappings)
        {
            if (category.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return group;
        }

        return "Other";
    }
}
