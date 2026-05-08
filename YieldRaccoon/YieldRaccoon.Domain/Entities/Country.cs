using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Entities;

/// <summary>
/// Lookup entity representing a country referenced by fund portfolio allocations.
/// </summary>
/// <remarks>
/// <para>
/// Grows organically: a new row is inserted the first time a country is encountered in
/// crawled portfolio data. <see cref="DisplayName"/> is the natural key (Swedish source
/// naming, e.g., "USA", "Kanada", "Tyskland") and is unique-indexed.
/// </para>
/// <para>
/// <see cref="CountryCode"/> is the ISO 3166-1 alpha-2 code (e.g., "US"). It may be null
/// for sources that don't provide it; on a re-crawl with a non-null code, the field is
/// backfilled but never overwritten with null.
/// </para>
/// </remarks>
[DebuggerDisplay("Country: {DisplayName} ({CountryCode})")]
public sealed class Country
{
    /// <summary>Primary key.</summary>
    public required CountryId Id { get; init; }

    /// <summary>Display name as it appears in the source payload (unique).</summary>
    public required string DisplayName { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code, or <c>null</c> if unknown.</summary>
    public string? CountryCode { get; set; }
}
