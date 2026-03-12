using Backend.API.Domain.FundData.Models;

namespace Backend.API.Domain.FundData.Interfaces;

/// <summary>
/// Repository for <see cref="FundHistoryRecord"/> persistence operations.
/// </summary>
public interface IFundHistoryRepository
{
    /// <summary>
    /// Upserts history records by (ISIN, NavDate) composite key.
    /// Used by fund-list crawl: overwrites existing daily snapshots.
    /// </summary>
    Task UpsertRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts history records only if no record exists for the same (ISIN, NavDate).
    /// Used by fund-about chart data: chart points are immutable once recorded.
    /// </summary>
    Task InsertIfNotExistsRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts history records using sparse (COALESCE) semantics.
    /// Used by the full-sync path: inserts new records with all fields; for existing (ISIN, NavDate) pairs,
    /// updates Capital/NumberOfOwners/Risk/SharpeRatio/StandardDeviation only when the incoming value is non-null.
    /// Nav and NavDate are never modified on existing records.
    /// </summary>
    Task UpsertSparseRangeAsync(IEnumerable<FundHistoryRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the store.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
