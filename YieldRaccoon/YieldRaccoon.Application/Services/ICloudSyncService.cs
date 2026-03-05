using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Orchestrates bulk-syncing local fund data (profiles + history records) to the Backend API.
/// </summary>
public interface ICloudSyncService
{
    /// <summary>
    /// Syncs fund profiles and their history records to the Backend API.
    /// </summary>
    /// <param name="companyNameFilter">
    /// Optional company name filter. When <c>null</c> or empty, all funds are synced.
    /// </param>
    /// <param name="throttleMs">Delay in milliseconds between per-fund API calls.</param>
    /// <param name="progress">Progress reporter for UI updates.</param>
    /// <param name="cancellationToken">Cancellation token — cancelled when the user closes the window.</param>
    /// <returns>Summary of the sync operation.</returns>
    Task<CloudSyncResult> SyncAsync(
        string? companyNameFilter,
        int throttleMs,
        IProgress<CloudSyncProgress> progress,
        CancellationToken cancellationToken = default);
}
