using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure.FundData.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFundHistoryRepository"/>.
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
        foreach (var record in records)
        {
            if (record.NavDate == null) continue;

            var existing = await _context.FundHistoryRecords
                .FirstOrDefaultAsync(h => h.IsinId == record.IsinId && h.NavDate == record.NavDate, cancellationToken);

            if (existing == null)
            {
                _context.FundHistoryRecords.Add(record);
            }
            else
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
        }
    }

    /// <inheritdoc />
    public async Task InsertIfNotExistsRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        foreach (var record in records)
        {
            if (record.NavDate == null) continue;

            var exists = await _context.FundHistoryRecords
                .AnyAsync(h => h.IsinId == record.IsinId && h.NavDate == record.NavDate, cancellationToken);

            if (!exists)
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
