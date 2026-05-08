using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.API.Domain.FundData.Interfaces;

/// <summary>
/// Repository for per-fund sector allocation rows.
/// </summary>
public interface IFundSectorAllocationRepository
{
    /// <summary>Loads all sector allocations for the given fund.</summary>
    Task<IReadOnlyList<FundSectorAllocation>> GetByFundAsync(IsinId isinId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracks a new allocation row for insert.</summary>
    Task AddAsync(FundSectorAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>Marks allocations for deletion.</summary>
    Task RemoveRangeAsync(IEnumerable<FundSectorAllocation> allocations,
        CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
