using System.Text.Json.Serialization;

namespace YieldRaccoon.Wpf.Models;

/// <summary>
/// Represents an intercepted API response containing a list of funds.
/// <para>
/// <strong>Pagination Behavior:</strong> When loading the next page, previous results
/// in the <see cref="Funds"/> collection are preserved and new results are added (accumulation).
/// The ViewModel handles duplicate detection by ISIN to prevent adding the same fund twice.
/// </para>
/// </summary>
public class InterceptedFundList
{
    /// <summary>
    /// Gets or sets the list of funds from the API fundListViews property.
    /// <para>
    /// This list is accumulated across multiple API calls when pagination is active.
    /// Each batch of funds loaded is added to the existing collection, creating a
    /// complete list of all loaded funds.
    /// </para>
    /// </summary>
    [JsonPropertyName("fundListViews")]
    public List<InterceptedFund>? Funds { get; set; }

    /// <summary>
    /// Gets or sets the total number of funds available (for pagination).
    /// This is parsed from the Swedish text "Visar X av Y" or similar.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current number of funds loaded so far.
    /// </summary>
    [JsonPropertyName("currentCount")]
    public int? CurrentCount { get; set; }

    /// <summary>
    /// Gets whether there are more funds to load.
    /// </summary>
    [JsonIgnore]
    public bool HasMore => CurrentCount.HasValue && TotalCount.HasValue && CurrentCount.Value < TotalCount.Value;
}