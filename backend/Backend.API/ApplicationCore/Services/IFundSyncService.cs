using Backend.API.ApplicationCore.DTOs;

namespace Backend.API.ApplicationCore.Services;

/// <summary>
/// Service for syncing fund data received from YieldRaccoon into Azure SQL.
/// </summary>
public interface IFundSyncService
{
    /// <summary>
    /// Syncs a batch of fund profiles + daily snapshots from a crawl session (fund list page).
    /// Profiles are upserted; history records are upserted by (ISIN, NavDate).
    /// </summary>
    Task<FundSyncResponse> SyncFromFundListAsync(FundListSyncRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs a single fund profile + chart history records from a fund detail page.
    /// Profile is upserted; history records are insert-only (skip existing NavDate).
    /// </summary>
    Task<FundSyncResponse> SyncFromFundAboutAsync(FundAboutSyncRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full-sync path used by CloudSyncWindow.
    /// Guarantees the fund profile FK exists (insert-if-not-exists, never overwrites existing profile).
    /// History records are upserted with sparse semantics: inserts new, updates only non-null sparse fields
    /// on existing records; Nav and NavDate are never modified.
    /// </summary>
    Task<FundSyncResponse> SyncFullHistoryAsync(FundFullHistorySyncRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs the latest country and sector portfolio allocations for a single fund.
    /// Diff-based upsert: insert new (fund, country/sector) pairs, update existing percentages,
    /// delete pairs that disappeared from the payload. Lookup tables grow organically.
    /// </summary>
    Task<FundSyncResponse> SyncPortfolioAllocationsAsync(FundPortfolioAllocationsSyncRequest request,
        CancellationToken cancellationToken = default);
}
