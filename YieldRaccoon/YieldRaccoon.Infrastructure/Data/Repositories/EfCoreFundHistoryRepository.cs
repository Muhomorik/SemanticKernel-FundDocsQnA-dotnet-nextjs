using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFundHistoryRepository"/>.
/// </summary>
/// <remarks>
/// Provides persistent storage for <see cref="FundHistoryRecord"/> entities using SQLite.
/// Duplicate detection uses FundId + NavDate composite key.
/// </remarks>
public class EfCoreFundHistoryRepository : IFundHistoryRepository
{
    private readonly YieldRaccoonDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreFundHistoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfCoreFundHistoryRepository(YieldRaccoonDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(FundHistoryRecord record, CancellationToken cancellationToken = default)
    {
        var existing = await _context.FundHistoryRecords
            .FirstOrDefaultAsync(h => h.IsinId == record.IsinId && h.NavDate == record.NavDate, cancellationToken);

        if (existing is not null)
        {
            // Remove existing record and add new one (since FundHistoryRecord has init-only properties)
            _context.FundHistoryRecords.Remove(existing);
        }

        await _context.FundHistoryRecords.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddOrUpdateRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        foreach (var record in records)
        {
            await AddOrUpdateAsync(record, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<int> AddRangeIfNotExistsAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        var insertedCount = 0;

        foreach (var record in records)
        {
            var exists = await _context.FundHistoryRecords
                .AnyAsync(h => h.IsinId == record.IsinId && h.NavDate == record.NavDate, cancellationToken);

            if (!exists)
            {
                await _context.FundHistoryRecords.AddAsync(record, cancellationToken);
                insertedCount++;
            }
        }

        return insertedCount;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
