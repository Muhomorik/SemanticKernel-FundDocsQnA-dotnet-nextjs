using YieldRaccoon.Domain.Entities;

namespace YieldRaccoon.Application.Repositories;

/// <summary>
/// Repository interface for persistent fund history record storage.
/// </summary>
/// <remarks>
/// <para>
/// This repository manages <see cref="FundHistoryRecord"/> entities in the database.
/// It provides async-only operations for adding or updating historical snapshots.
/// Duplicate detection uses FundId + NavDate composite key.
/// </para>
/// </remarks>
public interface IFundHistoryRepository
{
    /// <summary>
    /// Adds or updates a history record based on FundId + NavDate composite key.
    /// If a record with the same FundId and NavDate exists, updates it; otherwise adds a new record.
    /// </summary>
    /// <param name="record">The history record to add or update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddOrUpdateAsync(FundHistoryRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates multiple history records based on FundId + NavDate composite key.
    /// </summary>
    /// <param name="records">The history records to add or update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddOrUpdateRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds records that do not already exist based on FundId + NavDate composite key.
    /// Existing records are silently skipped (never overwritten).
    /// </summary>
    /// <param name="records">The history records to insert if not already present.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of records actually inserted.</returns>
    Task<int> AddRangeIfNotExistsAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
