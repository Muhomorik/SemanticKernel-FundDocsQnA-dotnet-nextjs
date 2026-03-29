using System.Reactive.Subjects;
using System.Text.Json;
using NLog;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Exceptions;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Mappers;
using YieldRaccoon.Infrastructure.Models;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Decorator that writes chart data to SQLite first, then syncs to the Backend API (fire-and-forget).
/// </summary>
/// <remarks>
/// <para>
/// After persisting chart history locally, builds a <see cref="FundAboutSyncRequest"/>
/// containing the fund profile (fetched from the repository) and chart history points
/// (re-parsed from the raw JSON in the page data slots).
/// </para>
/// <para>
/// Re-parsing the JSON is intentional: it avoids coupling with the inner service's internals
/// and is cheap since chart payloads are small (~KB each).
/// </para>
/// </remarks>
public class DualWriteChartIngestionService : IAboutFundChartIngestionService
{
    private readonly ILogger _logger;
    private readonly IAboutFundChartIngestionService _inner;
    private readonly IFundSyncApiClient _apiClient;
    private readonly IFundProfileRepository _profileRepository;
    private readonly Subject<BackendSyncStatus> _syncStatus;

    /// <summary>
    /// Stockholm timezone for converting chart timestamps to NAV dates.
    /// </summary>
    private static readonly TimeZoneInfo StockholmTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    public DualWriteChartIngestionService(
        ILogger logger,
        IAboutFundChartIngestionService inner,
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
    public async Task<int> IngestChartDataAsync(
        AboutFundPageData pageData,
        IsinId isinId,
        CancellationToken cancellationToken = default)
    {
        // 1. SQLite — always runs, exceptions propagate normally
        var insertedCount = await _inner.IngestChartDataAsync(pageData, isinId, cancellationToken);

        // 2. Backend API — fire-and-forget, never blocks the caller
        _ = SyncToBackendAsync(pageData, isinId);

        return insertedCount;
    }

    private async Task SyncToBackendAsync(AboutFundPageData pageData, IsinId isinId)
    {
        try
        {
            // Fetch the profile to include in the API request
            var profile = await _profileRepository.GetByIsinAsync(isinId);
            if (profile is null)
            {
                _logger.Warn("Cannot sync about-fund to backend: profile not found for {0}", isinId.Isin);
                _syncStatus.OnNext(BackendSyncStatus.Error($"Profile not found for {isinId.Isin}"));
                return;
            }

            var apiProfile = profile.ToApiFundListDto();
            var historyPoints = ParseChartHistoryPoints(pageData, isinId);

            var request = new FundAboutSyncRequest
            {
                Profile = apiProfile,
                HistoryRecords = historyPoints
            };

            var response = await _apiClient.SyncFundAboutAsync(request);

            _logger.Info("Backend about-fund sync completed for {0}: {1} history records inserted",
                isinId.Isin, response.HistoryRecordsInserted);
            _syncStatus.OnNext(BackendSyncStatus.Success(
                $"Synced {isinId.Isin}: {response.HistoryRecordsInserted} history records"));
        }
        catch (RateLimitedException ex)
        {
            _logger.Warn(ex, "Backend rate limited during about-fund sync for {0}", isinId.Isin);
            _syncStatus.OnNext(BackendSyncStatus.Error(
                $"Rate limited by backend ({isinId.Isin}) — retries exhausted"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Backend sync failed for about-fund {0}", isinId.Isin);
            _syncStatus.OnNext(BackendSyncStatus.Error($"Sync error ({isinId.Isin}): {ex.Message}"));
        }
    }

    /// <summary>
    /// Re-parses chart JSON from succeeded slots, deduplicates by NavDate,
    /// and maps to API DTOs.
    /// </summary>
    private List<ApiFundHistoryPointDto> ParseChartHistoryPoints(
        AboutFundPageData pageData,
        IsinId isinId)
    {
        var seen = new HashSet<DateOnly>();
        var points = new List<ApiFundHistoryPointDto>();

        foreach (var (slot, data) in pageData.AllSlots())
        {
            if (!data.IsSucceeded || string.IsNullOrWhiteSpace(data.Data))
                continue;

            try
            {
                var response = JsonSerializer.Deserialize<AboutFundChartResponse>(data.Data!);
                if (response?.DataSerie is null or { Count: 0 })
                    continue;

                foreach (var point in response.DataSerie)
                {
                    var navDate = ConvertToDateOnly(point.X);
                    if (!seen.Add(navDate))
                        continue;

                    points.Add(new ApiFundHistoryPointDto
                    {
                        Isin = isinId.Isin,
                        Nav = point.Y,
                        NavDate = navDate.ToString("yyyy-MM-dd")
                    });
                }
            }
            catch (JsonException ex)
            {
                _logger.Warn("Failed to deserialize chart slot {0} for backend sync: {1}", slot, ex.Message);
            }
        }

        return points;
    }

    private static DateOnly ConvertToDateOnly(long unixMilliseconds)
    {
        var dto = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
        var stockholmTime = TimeZoneInfo.ConvertTime(dto, StockholmTimeZone);
        return DateOnly.FromDateTime(stockholmTime.DateTime);
    }
}
