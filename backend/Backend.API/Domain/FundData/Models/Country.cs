using System.Diagnostics;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.API.Domain.FundData.Models;

/// <summary>
/// Lookup entity representing a country referenced by fund portfolio allocations.
/// </summary>
/// <remarks>
/// Grows organically as new countries are encountered in synced portfolio data.
/// <see cref="DisplayName"/> is the natural key (unique-indexed).
/// </remarks>
[DebuggerDisplay("Country: {DisplayName} ({CountryCode})")]
public sealed class Country
{
    /// <summary>Primary key.</summary>
    public required CountryId Id { get; init; }

    /// <summary>Display name (Swedish source naming, unique).</summary>
    public required string DisplayName { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code; nullable.</summary>
    public string? CountryCode { get; set; }
}
