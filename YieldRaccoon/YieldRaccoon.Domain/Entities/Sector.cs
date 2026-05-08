using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Entities;

/// <summary>
/// Lookup entity representing a sector referenced by fund portfolio allocations.
/// </summary>
/// <remarks>
/// Grows organically: a new row is inserted the first time a sector is encountered in
/// crawled portfolio data. <see cref="DisplayName"/> is the natural key (Swedish source
/// naming, e.g., "Teknik", "Råvaror", "Industri") and is unique-indexed.
/// </remarks>
[DebuggerDisplay("Sector: {DisplayName}")]
public sealed class Sector
{
    /// <summary>Primary key.</summary>
    public required SectorId Id { get; init; }

    /// <summary>Display name as it appears in the source payload (unique).</summary>
    public required string DisplayName { get; set; }
}
