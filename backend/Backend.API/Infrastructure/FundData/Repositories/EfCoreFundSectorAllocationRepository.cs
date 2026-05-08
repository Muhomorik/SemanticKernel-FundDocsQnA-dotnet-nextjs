using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure.FundData.Repositories;

/// <summary>EF Core implementation of <see cref="IFundSectorAllocationRepository"/>.</summary>
public class EfCoreFundSectorAllocationRepository : IFundSectorAllocationRepository
{
    private readonly FundDataDbContext _context;

    public EfCoreFundSectorAllocationRepository(FundDataDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FundSectorAllocation>> GetByFundAsync(IsinId isinId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FundSectorAllocations
            .Where(a => a.IsinId == isinId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(FundSectorAllocation allocation, CancellationToken cancellationToken = default)
    {
        _context.FundSectorAllocations.Add(allocation);
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
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
