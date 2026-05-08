using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Entities;

/// <summary>
/// Latest portfolio allocation of a fund to a given country.
/// </summary>
/// <remarks>
/// <para>
/// One row per <c>(IsinId, CountryId)</c> pair. Re-ingested on each crawl: existing rows
/// updated with the new percentage; pairs that disappeared from the payload are deleted.
/// No history is retained — only the latest snapshot.
/// </para>
/// <para>
/// Cascade-deleted with <see cref="FundProfile"/>; restricted-on-delete from
/// <see cref="Country"/> (don't drop a lookup row referenced by any fund).
/// </para>
/// </remarks>
[DebuggerDisplay("FundCountryAllocation: {IsinId} → {CountryId} = {Percentage}%")]
public sealed class FundCountryAllocation
{
    /// <summary>Primary key.</summary>
    public required FundCountryAllocationId Id { get; init; }

    /// <summary>FK to the owning <see cref="FundProfile"/>.</summary>
    public required IsinId IsinId { get; init; }

    /// <summary>FK to the <see cref="Country"/> lookup row.</summary>
    public required CountryId CountryId { get; set; }

    /// <summary>Percentage of the fund's portfolio allocated to this country (0–100).</summary>
    public required decimal Percentage { get; set; }
}
