using System.Reactive.Subjects;
using NLog;
using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Infrastructure.Mappers;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Decorator that writes fund data to SQLite first, then syncs to the Backend API (fire-and-forget).
/// </summary>
/// <remarks>
/// <para>
/// The SQLite write always completes and returns to the caller immediately.
/// The Backend API call runs asynchronously and never blocks or throws to the caller.
/// Errors are logged and published to the status stream for UI display.
/// </para>
/// </remarks>
public class DualWriteFundIngestionService : IFundIngestionService
{
    private readonly ILogger _logger;
    private readonly IFundIngestionService _inner;
    private readonly IFundSyncApiClient _apiClient;
    private readonly Subject<BackendSyncStatus> _syncStatus;

    public DualWriteFundIngestionService(
        ILogger logger,
        IFundIngestionService inner,
        IFundSyncApiClient apiClient,
        Subject<BackendSyncStatus> syncStatus)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _syncStatus = syncStatus ?? throw new ArgumentNullException(nameof(syncStatus));
    }

    /// <inheritdoc />
    public async Task<int> IngestBatchAsync(
        IEnumerable<FundDataDto> fundDataList,
        CancellationToken cancellationToken = default)
    {
        // Materialize so we can consume the enumerable twice (SQLite + API)
        var funds = fundDataList as IList<FundDataDto> ?? fundDataList.ToList();

        // 1. SQLite — always runs, exceptions propagate normally
        var count = await _inner.IngestBatchAsync(funds, cancellationToken);

        // 2. Backend API — fire-and-forget, never blocks the caller
        _ = SyncToBackendAsync(funds);

        return count;
    }

    private async Task SyncToBackendAsync(IList<FundDataDto> funds)
    {
        try
        {
            var apiFunds = funds
                .Where(d => !string.IsNullOrWhiteSpace(d.Isin) && !string.IsNullOrWhiteSpace(d.Name))
                .Select(d => d.ToApiFundDto())
                .ToList();

            if (apiFunds.Count == 0)
            {
                _logger.Debug("No valid funds to sync to backend — skipping");
                return;
            }

            var request = new FundListSyncRequest { Funds = apiFunds };
            var response = await _apiClient.SyncFundListAsync(request);

            _logger.Info("Backend sync completed: {0} profiles processed", response.ProfilesProcessed);
            _syncStatus.OnNext(BackendSyncStatus.Success(
                $"Synced {response.ProfilesProcessed} funds"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Backend sync failed for fund list");
            _syncStatus.OnNext(BackendSyncStatus.Error($"Sync error: {ex.Message}"));
        }
    }
}
