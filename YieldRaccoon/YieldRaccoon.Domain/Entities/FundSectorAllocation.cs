using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Entities;

/// <summary>
/// Latest portfolio allocation of a fund to a given sector.
/// </summary>
/// <remarks>
/// <para>
/// One row per <c>(IsinId, SectorId)</c> pair. Re-ingested on each crawl: existing rows
/// updated with the new percentage; pairs that disappeared from the payload are deleted.
/// No history is retained — only the latest snapshot.
/// </para>
/// <para>
/// Cascade-deleted with <see cref="FundProfile"/>; restricted-on-delete from
/// <see cref="Sector"/> (don't drop a lookup row referenced by any fund).
/// </para>
/// </remarks>
[DebuggerDisplay("FundSectorAllocation: {IsinId} → {SectorId} = {Percentage}%")]
public sealed class FundSectorAllocation
{
    /// <summary>Primary key.</summary>
    public required FundSectorAllocationId Id { get; init; }

    /// <summary>FK to the owning <see cref="FundProfile"/>.</summary>
    public required IsinId IsinId { get; init; }

    /// <summary>FK to the <see cref="Sector"/> lookup row.</summary>
    public required SectorId SectorId { get; set; }

    /// <summary>Percentage of the fund's portfolio allocated to this sector (0–100).</summary>
    public required decimal Percentage { get; set; }
}
