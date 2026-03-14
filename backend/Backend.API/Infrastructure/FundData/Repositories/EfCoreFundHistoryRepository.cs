using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure.FundData.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFundHistoryRepository"/>.
/// Batch-loads existing records to avoid N+1 query patterns.
/// </summary>
public class EfCoreFundHistoryRepository : IFundHistoryRepository
{
    private readonly FundDataDbContext _context;

    public EfCoreFundHistoryRepository(FundDataDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task UpsertRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        var recordsList = records.Where(r => r.NavDate != null).ToList();
        if (recordsList.Count == 0) return;

        // Batch-load all potentially matching existing records in one query.
        // Generates: WHERE FundId IN (...) AND NavDate IN (...)
        var isins = recordsList.Select(r => r.IsinId).Distinct().ToList();
        var navDates = recordsList.Select(r => r.NavDate!.Value).Distinct().ToList();

        var existingRecords = await _context.FundHistoryRecords
            .Where(h => isins.Contains(h.IsinId) && navDates.Contains(h.NavDate!.Value))
            .ToListAsync(cancellationToken);

        var lookup = existingRecords.ToDictionary(h => (h.IsinId, h.NavDate!.Value));

        foreach (var record in recordsList)
        {
            if (lookup.TryGetValue((record.IsinId, record.NavDate!.Value), out var existing))
            {
                // Overwrite daily snapshot values
                _context.Entry(existing).CurrentValues.SetValues(new
                {
                    existing.Id,       // Keep existing PK
                    record.IsinId,
                    record.Nav,
                    record.NavDate,
                    record.Capital,
                    record.NumberOfOwners,
                    record.Risk,
                    record.SharpeRatio,
                    record.StandardDeviation
                });
            }
            else
            {
                _context.FundHistoryRecords.Add(record);
            }
        }
    }

    /// <inheritdoc />
    public async Task InsertIfNotExistsRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        var recordsList = records.Where(r => r.NavDate != null).ToList();
        if (recordsList.Count == 0) return;

        // Batch-load existing (ISIN, NavDate) pairs in one query
        var isins = recordsList.Select(r => r.IsinId).Distinct().ToList();
        var navDates = recordsList.Select(r => r.NavDate!.Value).Distinct().ToList();

        var existingPairs = await _context.FundHistoryRecords
            .Where(h => isins.Contains(h.IsinId) && navDates.Contains(h.NavDate!.Value))
            .Select(h => new { h.IsinId, NavDate = h.NavDate!.Value })
            .ToListAsync(cancellationToken);

        var existingSet = existingPairs.Select(p => (p.IsinId, p.NavDate)).ToHashSet();

        foreach (var record in recordsList)
        {
            if (!existingSet.Contains((record.IsinId, record.NavDate!.Value)))
            {
                _context.FundHistoryRecords.Add(record);
            }
        }
    }

    /// <inheritdoc />
    public async Task UpsertSparseRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        var recordsList = records.Where(r => r.NavDate != null).ToList();
        if (recordsList.Count == 0) return;

        // Batch-load all potentially matching existing records in one query.
        var isins = recordsList.Select(r => r.IsinId).Distinct().ToList();
        var navDates = recordsList.Select(r => r.NavDate!.Value).Distinct().ToList();

        var existingRecords = await _context.FundHistoryRecords
            .Where(h => isins.Contains(h.IsinId) && navDates.Contains(h.NavDate!.Value))
            .ToListAsync(cancellationToken);

        var lookup = existingRecords.ToDictionary(h => (h.IsinId, h.NavDate!.Value));

        foreach (var record in recordsList)
        {
            if (lookup.TryGetValue((record.IsinId, record.NavDate!.Value), out var existing))
            {
                // Sparse update: only overwrite when incoming value is non-null.
                // Nav and NavDate are never touched on existing records.
                var entry = _context.Entry(existing);
                if (record.Capital != null)
                    entry.Property(p => p.Capital).CurrentValue = record.Capital;
                if (record.NumberOfOwners != null)
                    entry.Property(p => p.NumberOfOwners).CurrentValue = record.NumberOfOwners;
                if (record.Risk != null)
                    entry.Property(p => p.Risk).CurrentValue = record.Risk;
                if (record.SharpeRatio != null)
                    entry.Property(p => p.SharpeRatio).CurrentValue = record.SharpeRatio;
                if (record.StandardDeviation != null)
                    entry.Property(p => p.StandardDeviation).CurrentValue = record.StandardDeviation;
            }
            else
            {
                _context.FundHistoryRecords.Add(record);
            }
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
