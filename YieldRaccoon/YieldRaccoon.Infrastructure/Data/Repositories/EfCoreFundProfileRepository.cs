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
            // Preserve fields not owned by the crawl ingestion pipeline.
            // AboutFundLastVisitedAt is set exclusively by the orchestrator;
            // FirstSeenAt is set once on insert and must never change.
            var preservedLastVisitedAt = existing.AboutFundLastVisitedAt;
            var preservedFirstSeenAt = existing.FirstSeenAt;

            _context.Entry(existing).CurrentValues.SetValues(fundProfile);

            existing.AboutFundLastVisitedAt = preservedLastVisitedAt;
            _context.Entry(existing).Property(e => e.FirstSeenAt).CurrentValue = preservedFirstSeenAt;
        }
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AboutFundScheduleItem>> GetFundsOrderedByLastVisitAsync(
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
    public async Task<string?> GetIsinByOrderBookIdAsync(OrderBookId orderBookId,
        CancellationToken cancellationToken = default)
    {
        return await _context.FundProfiles
            .Where(fp => fp.OrderbookId == orderBookId.Value)
            .Select(fp => fp.Id.Isin)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FundProfile?> GetByIsinAsync(IsinId isinId, CancellationToken cancellationToken = default)
    {
        return await _context.FundProfiles.FindAsync(new object[] { isinId }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FundProfile>> GetByCompanyNameFilterAsync(
        string? companyName, CancellationToken cancellationToken = default)
    {
        var query = _context.FundProfiles
            .Include(fp => fp.HistoryRecords)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            var filter = companyName.Trim();
            query = query.Where(fp => fp.CompanyName != null
                                      && EF.Functions.Like(fp.CompanyName, $"%{filter}%"));
        }

        return await query.ToListAsync(cancellationToken);
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