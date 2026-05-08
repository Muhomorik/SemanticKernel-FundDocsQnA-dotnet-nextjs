using System.Diagnostics;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.API.Domain.FundData.Models;

/// <summary>
/// Latest portfolio allocation of a fund to a given sector (no history).
/// </summary>
[DebuggerDisplay("FundSectorAllocation: {IsinId} → {SectorId} = {Percentage}%")]
public sealed class FundSectorAllocation
{
    /// <summary>Primary key.</summary>
    public required FundSectorAllocationId Id { get; init; }

    /// <summary>FK to <see cref="FundProfile"/>.</summary>
    public required IsinId IsinId { get; init; }

    /// <summary>FK to <see cref="Sector"/>.</summary>
    public required SectorId SectorId { get; set; }

    /// <summary>Percentage of the fund's portfolio (0–100).</summary>
    public required decimal Percentage { get; set; }
}
