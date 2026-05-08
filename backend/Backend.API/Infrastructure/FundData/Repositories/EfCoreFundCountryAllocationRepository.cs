using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure.FundData.Repositories;

/// <summary>EF Core implementation of <see cref="IFundCountryAllocationRepository"/>.</summary>
public class EfCoreFundCountryAllocationRepository : IFundCountryAllocationRepository
{
    private readonly FundDataDbContext _context;

    public EfCoreFundCountryAllocationRepository(FundDataDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FundCountryAllocation>> GetByFundAsync(IsinId isinId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FundCountryAllocations
            .Where(a => a.IsinId == isinId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(FundCountryAllocation allocation, CancellationToken cancellationToken = default)
    {
        _context.FundCountryAllocations.Add(allocation);
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
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
