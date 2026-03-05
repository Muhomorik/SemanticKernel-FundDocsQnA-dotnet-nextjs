using System.Reactive.Subjects;
using NLog;
using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Exceptions;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;
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
    private readonly IFundProfileRepository _profileRepository;
    private readonly Subject<BackendSyncStatus> _syncStatus;

    public DualWriteFundIngestionService(
        ILogger logger,
        IFundIngestionService inner,
        IFundSyncApiClient apiClient,
        IFundProfileRepository profileRepository,
        Subject<BackendSyncStatus> syncStatus)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
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
            // Fetch profiles from local DB to include authoritative timestamps
            var apiFunds = new List<ApiFundDto>();
            foreach (var dto in funds)
            {
                if (string.IsNullOrWhiteSpace(dto.Isin) || string.IsNullOrWhiteSpace(dto.Name))
                    continue;

                IsinId isinId;
                try { isinId = IsinId.Create(dto.Isin); }
                catch (ArgumentException) { continue; }

                var profile = await _profileRepository.GetByIsinAsync(isinId);

                // Always start from the DTO (which carries Nav/NavDate from the listing page).
                // FundProfile does not have Nav/NavDate — using profile.ToApiFundDto() would
                // produce null values, causing the backend to silently skip history records.
                var apiDto = dto.ToApiFundDto();
                if (profile != null)
                {
                    apiDto = apiDto with
                    {
                        FirstSeenAt = profile.FirstSeenAt.ToString("O"),
                        CrawlerLastUpdatedAt = profile.CrawlerLastUpdatedAt?.ToString("O"),
                        AboutFundLastVisitedAt = profile.AboutFundLastVisitedAt?.ToString("O"),
                    };
                }
                apiFunds.Add(apiDto);
            }

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
        catch (RateLimitedException ex)
        {
            _logger.Warn(ex, "Backend rate limited during fund list sync");
            _syncStatus.OnNext(BackendSyncStatus.Error("Rate limited by backend — retries exhausted"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Backend sync failed for fund list");
            _syncStatus.OnNext(BackendSyncStatus.Error($"Sync error: {ex.Message}"));
        }
    }
}
