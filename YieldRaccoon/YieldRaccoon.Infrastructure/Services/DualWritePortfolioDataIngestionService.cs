using System.Reactive.Subjects;
using System.Text.Json;
using NLog;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Exceptions;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Models;


namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Decorator that writes portfolio allocations to SQLite first, then syncs to the Backend API.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the <see cref="DualWriteChartIngestionService"/> pattern: local persistence runs
/// synchronously and exceptions propagate; the cloud sync is fire-and-forget so backend
/// outages never break the local crawl pipeline.
/// </para>
/// <para>
/// The cloud payload is rebuilt by re-parsing <see cref="AboutFundPageData.PortfolioDataJson"/>
/// rather than reading from the DB — this keeps us decoupled from the inner service's
/// internals and avoids an extra round-trip.
/// </para>
/// </remarks>
public class DualWritePortfolioDataIngestionService : IPortfolioDataIngestionService
{
    private readonly ILogger _logger;
    private readonly IPortfolioDataIngestionService _inner;
    private readonly IFundSyncApiClient _apiClient;
    private readonly Subject<BackendSyncStatus> _syncStatus;

    /// <summary>
    /// Initializes a new instance of the <see cref="DualWritePortfolioDataIngestionService"/> class.
    /// </summary>
    public DualWritePortfolioDataIngestionService(
        ILogger logger,
        IPortfolioDataIngestionService inner,
        IFundSyncApiClient apiClient,
        Subject<BackendSyncStatus> syncStatus)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _syncStatus = syncStatus ?? throw new ArgumentNullException(nameof(syncStatus));
    }

    /// <inheritdoc />
    public async Task<int> IngestPortfolioDataAsync(
        AboutFundPageData pageData,
        IsinId isinId,
        CancellationToken cancellationToken = default)
    {
        // 1. SQLite — exceptions propagate normally
        var rowsTouched = await _inner.IngestPortfolioDataAsync(pageData, isinId, cancellationToken);

        // 2. Backend API — fire-and-forget (only if there's something to send)
        if (rowsTouched > 0 || !string.IsNullOrEmpty(pageData.PortfolioDataJson))
            _ = SyncToBackendAsync(pageData, isinId);

        return rowsTouched;
    }

    private async Task SyncToBackendAsync(AboutFundPageData pageData, IsinId isinId)
    {
        try
        {
            var request = BuildRequest(pageData, isinId);
            if (request is null)
                return; // payload was empty/malformed — local already logged

            var response = await _apiClient.SyncPortfolioAllocationsAsync(request);

            _logger.Info("Backend portfolio-allocations sync completed for {0}: {1} countries + {2} sectors",
                isinId.Isin, request.Countries.Count, request.Sectors.Count);
            _syncStatus.OnNext(BackendSyncStatus.Success(
                $"Synced allocations {isinId.Isin}: {request.Countries.Count}c/{request.Sectors.Count}s"));
        }
        catch (RateLimitedException ex)
        {
            _logger.Warn(ex, "Backend rate limited during portfolio-allocations sync for {0}", isinId.Isin);
            _syncStatus.OnNext(BackendSyncStatus.Error(
                $"Rate limited by backend (allocations {isinId.Isin}) — retries exhausted"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Backend sync failed for portfolio-allocations {0}", isinId.Isin);
            _syncStatus.OnNext(BackendSyncStatus.Error(
                $"Allocations sync error ({isinId.Isin}): {ex.Message}"));
        }
    }

    private FundPortfolioAllocationsSyncRequest? BuildRequest(AboutFundPageData pageData, IsinId isinId)
    {
        if (string.IsNullOrEmpty(pageData.PortfolioDataJson))
            return null;

        PortfolioDataResponse? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PortfolioDataResponse>(pageData.PortfolioDataJson);
        }
        catch (JsonException ex)
        {
            _logger.Warn(ex, "Failed to deserialize portfolio-data JSON for backend sync ({0})", isinId.Isin);
            return null;
        }

        if (dto is null)
            return null;

        var countries = (dto.CountryChartData ?? Array.Empty<CountryChartDataItem>())
            .Select(c => new ApiCountryAllocationDto
            {
                DisplayName = c.DisplayName,
                CountryCode = c.CountryCode,
                Percentage = c.Percentage
            })
            .ToList();

        var sectors = (dto.SectorChartData ?? Array.Empty<SectorChartDataItem>())
            .Select(s => new ApiSectorAllocationDto
            {
                DisplayName = s.DisplayName,
                Percentage = s.Percentage
            })
            .ToList();

        if (countries.Count == 0 && sectors.Count == 0)
            return null;

        return new FundPortfolioAllocationsSyncRequest
        {
            Isin = isinId.Isin,
            Countries = countries,
            Sectors = sectors
        };
    }
}
