using YieldRaccoon.Application.DTOs.Api;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// HTTP client abstraction for the Backend API fund sync endpoints.
/// </summary>
/// <remarks>
/// Used by the DualWrite decorators to push fund data to the cloud.
/// Implementations handle HTTP transport, serialization, and authentication.
/// </remarks>
public interface IFundSyncApiClient
{
    /// <summary>
    /// Gets whether the API client has a configured Backend API URL.
    /// </summary>
    bool IsConfigured { get; }
    /// <summary>
    /// Sends a batch of fund profiles to <c>POST /api/funds/list</c>.
    /// </summary>
    /// <param name="request">The batch of fund data from a crawl session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sync result with counts of processed profiles and history records.</returns>
    Task<FundSyncResponse> SyncFundListAsync(
        FundListSyncRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a fund profile and chart history to <c>POST /api/funds/about</c>.
    /// </summary>
    /// <param name="request">The fund profile and chart history from an about-fund page visit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sync result with counts of processed profiles and history records.</returns>
    Task<FundSyncResponse> SyncFundAboutAsync(
        FundAboutSyncRequest request,
        CancellationToken cancellationToken = default);
}
