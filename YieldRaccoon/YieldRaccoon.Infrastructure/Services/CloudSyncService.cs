using System.Diagnostics;
using NLog;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Infrastructure.Mappers;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Orchestrates bulk-syncing local fund data to the Backend API.
/// </summary>
/// <remarks>
/// <para>
/// Two-phase sync:
/// <list type="number">
///   <item>Batch all matched profiles in a single <c>POST /api/funds/list</c> call.</item>
///   <item>Per-fund history sync via <c>POST /api/funds/about</c> with configurable throttle delay.</item>
/// </list>
/// </para>
/// </remarks>
public class CloudSyncService : ICloudSyncService
{
    private readonly ILogger _logger;
    private readonly IFundProfileRepository _profileRepository;
    private readonly IFundSyncApiClient _apiClient;

    public CloudSyncService(
        ILogger logger,
        IFundProfileRepository profileRepository,
        IFundSyncApiClient apiClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <inheritdoc />
    public async Task<CloudSyncResult> SyncAsync(
        string? companyNameFilter,
        int throttleMs,
        IProgress<CloudSyncProgress> progress,
        CancellationToken cancellationToken = default)
    {
        if (!_apiClient.IsConfigured)
            throw new InvalidOperationException(
                "Backend API URL is not configured. Set the URL in Settings before syncing.");

        var stopwatch = Stopwatch.StartNew();
        var profilesSynced = 0;
        var successCount = 0;
        var failCount = 0;
        var historyTotal = 0;

        try
        {
            // Query phase
            progress.Report(new CloudSyncProgress
            {
                TotalFunds = 0,
                ProcessedFunds = 0,
                SuccessCount = 0,
                FailCount = 0,
                CurrentFundName = string.Empty,
                Phase = "Querying..."
            });

            var funds = await _profileRepository.GetByCompanyNameFilterAsync(companyNameFilter, cancellationToken);

            if (funds.Count == 0)
            {
                _logger.Info("Cloud sync: no funds matched filter '{0}'", companyNameFilter ?? "(all)");
                return CreateResult(0, 0, 0, 0, false, stopwatch.Elapsed);
            }

            _logger.Info("Cloud sync: {0} funds matched filter '{1}'", funds.Count, companyNameFilter ?? "(all)");

            // Phase 1 — Batch profile sync
            progress.Report(new CloudSyncProgress
            {
                TotalFunds = funds.Count,
                ProcessedFunds = 0,
                SuccessCount = 0,
                FailCount = 0,
                CurrentFundName = string.Empty,
                Phase = "Syncing profiles..."
            });

            var apiFunds = funds.Select(fp => fp.ToApiFundDto()).ToList();
            var listRequest = new FundListSyncRequest { Funds = apiFunds };
            var listResponse = await _apiClient.SyncFundListAsync(listRequest, cancellationToken);
            profilesSynced = listResponse.ProfilesProcessed;

            _logger.Info("Cloud sync phase 1: {0} profiles synced", profilesSynced);

            // Phase 2 — Per-fund history sync with throttling
            for (var i = 0; i < funds.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fund = funds[i];
                var processed = i + 1;

                progress.Report(new CloudSyncProgress
                {
                    TotalFunds = funds.Count,
                    ProcessedFunds = i,
                    SuccessCount = successCount,
                    FailCount = failCount,
                    CurrentFundName = fund.Name,
                    Phase = $"Syncing history ({processed}/{funds.Count})"
                });

                try
                {
                    var apiProfile = fund.ToApiFundDto();
                    var historyPoints = fund.HistoryRecords
                        .Select(hr => new ApiFundHistoryPointDto
                        {
                            Isin = hr.IsinId.Isin,
                            Nav = hr.Nav,
                            NavDate = hr.NavDate?.ToString("yyyy-MM-dd")
                        })
                        .ToList();

                    var aboutRequest = new FundAboutSyncRequest
                    {
                        Profile = apiProfile,
                        HistoryRecords = historyPoints
                    };

                    var aboutResponse = await _apiClient.SyncFundAboutAsync(aboutRequest, cancellationToken);
                    historyTotal += aboutResponse.HistoryRecordsInserted;
                    successCount++;
                }
                catch (OperationCanceledException)
                {
                    throw; // Let cancellation propagate
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Cloud sync: failed to sync fund {0} ({1})", fund.Name, fund.Id.Isin);
                    failCount++;
                }

                // Throttle between calls (skip after last fund)
                if (i < funds.Count - 1 && throttleMs > 0)
                {
                    await Task.Delay(throttleMs, cancellationToken);
                }
            }

            // Final progress
            progress.Report(new CloudSyncProgress
            {
                TotalFunds = funds.Count,
                ProcessedFunds = funds.Count,
                SuccessCount = successCount,
                FailCount = failCount,
                CurrentFundName = string.Empty,
                Phase = "Completed"
            });

            stopwatch.Stop();
            _logger.Info("Cloud sync completed: {0} profiles, {1} history records, {2} failed, {3:mm\\:ss}",
                profilesSynced, historyTotal, failCount, stopwatch.Elapsed);

            return CreateResult(funds.Count, profilesSynced, historyTotal, failCount, false, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.Info("Cloud sync cancelled after {0} funds ({1} success, {2} failed)",
                successCount + failCount, successCount, failCount);

            return CreateResult(
                successCount + failCount, profilesSynced, historyTotal, failCount, true, stopwatch.Elapsed);
        }
    }

    private static CloudSyncResult CreateResult(
        int totalFunds, int profilesSynced, int historyRecordsSynced,
        int failedFunds, bool wasCancelled, TimeSpan duration) => new()
    {
        TotalFunds = totalFunds,
        ProfilesSynced = profilesSynced,
        HistoryRecordsSynced = historyRecordsSynced,
        FailedFunds = failedFunds,
        WasCancelled = wasCancelled,
        Duration = duration
    };
}
