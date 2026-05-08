using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Repositories;

/// <summary>
/// Repository for per-fund sector allocation rows.
/// </summary>
public interface IFundSectorAllocationRepository
{
    /// <summary>Loads all sector allocation rows for the given fund.</summary>
    Task<IReadOnlyList<FundSectorAllocation>> GetByFundAsync(IsinId isinId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracks a new allocation row for insert.</summary>
    Task AddAsync(FundSectorAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>Marks a tracked allocation row as modified.</summary>
    Task UpdateAsync(FundSectorAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>Marks the given allocation rows for deletion.</summary>
    Task RemoveRangeAsync(IEnumerable<FundSectorAllocation> allocations,
        CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
