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
}
