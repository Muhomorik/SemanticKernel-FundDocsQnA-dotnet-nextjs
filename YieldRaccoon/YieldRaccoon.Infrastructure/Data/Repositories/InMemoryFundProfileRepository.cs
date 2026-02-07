using System.Collections.Concurrent;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// In-memory implementation of <see cref="IFundProfileRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides volatile, session-scoped storage for <see cref="FundProfile"/> entities.
/// Data is lost when the application restarts.
/// </para>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe operations.
/// </para>
/// </remarks>
public class InMemoryFundProfileRepository : IFundProfileRepository
{
    private readonly ConcurrentDictionary<FundId, FundProfile> _profiles = new();

    /// <inheritdoc />
    public Task AddOrUpdateAsync(FundProfile fundProfile, CancellationToken cancellationToken = default)
    {
        _profiles.AddOrUpdate(fundProfile.Id, fundProfile, (_, _) => fundProfile);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // No-op for in-memory storage - changes are applied immediately
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AboutFundScheduleItem>> GetFundsOrderedByHistoryCountAsync(
        int limit = 60, CancellationToken cancellationToken = default)
    {
        // In-memory implementation returns all profiles with zero history count
        // since history records are tracked in a separate in-memory repository.
        var items = _profiles.Values
            .Select(fp => new AboutFundScheduleItem
            {
                Isin = fp.Id.Isin,
                OrderbookId = fp.OrderbookId,
                Name = fp.Name,
                HistoryRecordCount = 0
            })
            .OrderBy(f => f.Name)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<AboutFundScheduleItem>>(items);
    }
}
