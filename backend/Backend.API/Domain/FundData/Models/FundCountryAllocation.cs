using System.Diagnostics;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.API.Domain.FundData.Models;

/// <summary>
/// Latest portfolio allocation of a fund to a given country (no history).
/// </summary>
[DebuggerDisplay("FundCountryAllocation: {IsinId} → {CountryId} = {Percentage}%")]
public sealed class FundCountryAllocation
{
    /// <summary>Primary key.</summary>
    public required FundCountryAllocationId Id { get; init; }

    /// <summary>FK to <see cref="FundProfile"/>.</summary>
    public required IsinId IsinId { get; init; }

    /// <summary>FK to <see cref="Country"/>.</summary>
    public required CountryId CountryId { get; set; }

    /// <summary>Percentage of the fund's portfolio (0–100).</summary>
    public required decimal Percentage { get; set; }
}
