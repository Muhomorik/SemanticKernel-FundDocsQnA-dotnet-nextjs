using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFundProfileRepository"/>.
/// </summary>
/// <remarks>
/// Provides persistent storage for <see cref="FundProfile"/> entities using SQLite.
/// </remarks>
public class EfCoreFundProfileRepository : IFundProfileRepository
{
    private readonly YieldRaccoonDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreFundProfileRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfCoreFundProfileRepository(YieldRaccoonDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(FundProfile fundProfile, CancellationToken cancellationToken = default)
    {
        var existing = await _context.FundProfiles.FindAsync(new object[] { fundProfile.Id }, cancellationToken);
        if (existing is null)
        {
            await _context.FundProfiles.AddAsync(fundProfile, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(fundProfile);
        }
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AboutFundScheduleItem>> GetFundsOrderedByHistoryCountAsync(
        int limit = 60, CancellationToken cancellationToken = default)
    {
        // Project and filter in SQL, then sort client-side because
        // SQLite cannot ORDER BY DateTimeOffset expressions.
        var rows = await _context.FundProfiles
            .Where(fp => fp.OrderbookId != null)
            .Select(fp => new
            {
                Isin = fp.Id.Isin,
                OrderbookId = fp.OrderbookId!,
                fp.Name,
                HistoryRecordCount = fp.HistoryRecords.Count,
                fp.AboutFundLastVisitedAt
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(f => f.AboutFundLastVisitedAt ?? DateTimeOffset.MinValue)
            .ThenBy(f => f.HistoryRecordCount)
            .Take(limit)
            .Select(f => new AboutFundScheduleItem
            {
                Isin = f.Isin,
                OrderBookId = OrderBookId.Create(f.OrderbookId),
                Name = f.Name,
                HistoryRecordCount = f.HistoryRecordCount,
                LastVisitedAt = f.AboutFundLastVisitedAt
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task UpdateLastVisitedAtAsync(IsinId isinId, DateTimeOffset visitedAt,
        CancellationToken cancellationToken = default)
    {
        var profile = await _context.FundProfiles.FindAsync(new object[] { isinId }, cancellationToken);
        if (profile is not null)
        {
            profile.AboutFundLastVisitedAt = visitedAt;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
