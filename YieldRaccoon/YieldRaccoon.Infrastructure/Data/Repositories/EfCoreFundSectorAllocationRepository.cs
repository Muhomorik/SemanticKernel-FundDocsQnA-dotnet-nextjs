using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFundSectorAllocationRepository"/>.
/// </summary>
public class EfCoreFundSectorAllocationRepository : IFundSectorAllocationRepository
{
    private readonly YieldRaccoonDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreFundSectorAllocationRepository"/> class.
    /// </summary>
    public EfCoreFundSectorAllocationRepository(YieldRaccoonDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FundSectorAllocation>> GetByFundAsync(IsinId isinId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FundSectorAllocations
            .Where(a => a.IsinId == isinId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(FundSectorAllocation allocation,
        CancellationToken cancellationToken = default)
    {
        await _context.FundSectorAllocations.AddAsync(allocation, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task UpdateAsync(FundSectorAllocation allocation,
        CancellationToken cancellationToken = default)
    {
        _context.FundSectorAllocations.Update(allocation);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveRangeAsync(IEnumerable<FundSectorAllocation> allocations,
        CancellationToken cancellationToken = default)
    {
        _context.FundSectorAllocations.RemoveRange(allocations);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
