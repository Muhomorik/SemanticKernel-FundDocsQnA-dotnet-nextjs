using System.Diagnostics;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.API.Domain.FundData.Models;

/// <summary>
/// Lookup entity representing a sector referenced by fund portfolio allocations.
/// </summary>
[DebuggerDisplay("Sector: {DisplayName}")]
public sealed class Sector
{
    /// <summary>Primary key.</summary>
    public required SectorId Id { get; init; }

    /// <summary>Display name (Swedish source naming, unique).</summary>
    public required string DisplayName { get; set; }
}
