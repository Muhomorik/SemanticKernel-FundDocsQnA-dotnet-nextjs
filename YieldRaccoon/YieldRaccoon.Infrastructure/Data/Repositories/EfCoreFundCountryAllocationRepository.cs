using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFundCountryAllocationRepository"/>.
/// </summary>
public class EfCoreFundCountryAllocationRepository : IFundCountryAllocationRepository
{
    private readonly YieldRaccoonDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreFundCountryAllocationRepository"/> class.
    /// </summary>
    public EfCoreFundCountryAllocationRepository(YieldRaccoonDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FundCountryAllocation>> GetByFundAsync(IsinId isinId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FundCountryAllocations
            .Where(a => a.IsinId == isinId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(FundCountryAllocation allocation,
        CancellationToken cancellationToken = default)
    {
        await _context.FundCountryAllocations.AddAsync(allocation, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task UpdateAsync(FundCountryAllocation allocation,
        CancellationToken cancellationToken = default)
    {
        // EF Core change tracker handles loaded entities; explicit Update is a no-op here
        // unless the entity was detached. Mark as Modified just in case.
        _context.FundCountryAllocations.Update(allocation);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveRangeAsync(IEnumerable<FundCountryAllocation> allocations,
        CancellationToken cancellationToken = default)
    {
        _context.FundCountryAllocations.RemoveRange(allocations);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
