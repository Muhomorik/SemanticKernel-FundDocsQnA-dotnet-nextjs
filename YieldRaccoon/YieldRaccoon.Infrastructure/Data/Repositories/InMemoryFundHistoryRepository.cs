using System.Collections.Concurrent;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// In-memory implementation of <see cref="IFundHistoryRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides volatile, session-scoped storage for <see cref="FundHistoryRecord"/> entities.
/// Data is lost when the application restarts.
/// </para>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> with thread-safe list operations.
/// Records are stored grouped by <see cref="IsinId"/> for efficient fund-based queries.
/// Duplicate detection uses FundId + NavDate composite key.
/// </para>
/// </remarks>
public class InMemoryFundHistoryRepository : IFundHistoryRepository
{
    private readonly ConcurrentDictionary<IsinId, List<FundHistoryRecord>> _records = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task AddOrUpdateAsync(FundHistoryRecord record, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var records = _records.GetOrAdd(record.IsinId, _ => new List<FundHistoryRecord>());

            // Find existing record with same NavDate
            var existingIndex = records.FindIndex(r => r.NavDate == record.NavDate);
            if (existingIndex >= 0)
            {
                // Replace existing record
                records[existingIndex] = record;
            }
            else
            {
                // Add new record
                records.Add(record);
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddOrUpdateRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        foreach (var record in records)
        {
            AddOrUpdateAsync(record, cancellationToken);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> AddRangeIfNotExistsAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        var insertedCount = 0;

        lock (_lock)
        {
            foreach (var record in records)
            {
                var list = _records.GetOrAdd(record.IsinId, _ => new List<FundHistoryRecord>());

                var exists = list.Exists(r => r.NavDate == record.NavDate);
                if (!exists)
                {
                    list.Add(record);
                    insertedCount++;
                }
            }
        }

        return Task.FromResult(insertedCount);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // No-op for in-memory storage - changes are applied immediately
        return Task.CompletedTask;
    }
}
