using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Repositories;

/// <summary>
/// Repository for per-fund country allocation rows.
/// </summary>
/// <remarks>
/// Exposes primitive add/update/remove operations; the diff-and-merge logic lives in
/// the ingestion service so the repository stays a thin EF Core wrapper.
/// </remarks>
public interface IFundCountryAllocationRepository
{
    /// <summary>Loads all country allocation rows for the given fund.</summary>
    Task<IReadOnlyList<FundCountryAllocation>> GetByFundAsync(IsinId isinId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracks a new allocation row for insert.</summary>
    Task AddAsync(FundCountryAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>Marks a tracked allocation row as modified.</summary>
    Task UpdateAsync(FundCountryAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>Marks the given allocation rows for deletion.</summary>
    Task RemoveRangeAsync(IEnumerable<FundCountryAllocation> allocations,
        CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
