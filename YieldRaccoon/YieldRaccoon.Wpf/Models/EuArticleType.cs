using System.Text.Json.Serialization;

namespace YieldRaccoon.Wpf.Models;

/// <summary>
/// Represents the EU article type for SFDR classification.
/// </summary>
public class EuArticleType
{
    /// <summary>
    /// Gets or sets the article name (e.g., "Artikel 6", "Artikel 8").
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the article value (e.g., "ARTICLE_TYPE_SIX", "ARTICLE_TYPE_EIGHT").
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
