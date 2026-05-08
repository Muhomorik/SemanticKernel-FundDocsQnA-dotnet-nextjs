using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.API.Domain.FundData.Interfaces;

/// <summary>
/// Repository for per-fund country allocation rows.
/// </summary>
public interface IFundCountryAllocationRepository
{
    /// <summary>Loads all country allocations for the given fund.</summary>
    Task<IReadOnlyList<FundCountryAllocation>> GetByFundAsync(IsinId isinId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracks a new allocation row for insert.</summary>
    Task AddAsync(FundCountryAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>Marks allocations for deletion.</summary>
    Task RemoveRangeAsync(IEnumerable<FundCountryAllocation> allocations,
        CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
