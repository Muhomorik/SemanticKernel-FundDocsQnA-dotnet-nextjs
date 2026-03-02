using Backend.API.ApplicationCore.DTOs;
using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.API.ApplicationCore.Services;

/// <summary>
/// Maps incoming API DTOs to domain entities and delegates to repositories.
/// </summary>
public class FundSyncService : IFundSyncService
{
    private readonly IFundProfileRepository _profileRepository;
    private readonly IFundHistoryRepository _historyRepository;
    private readonly ILogger<FundSyncService> _logger;

    public FundSyncService(
        IFundProfileRepository profileRepository,
        IFundHistoryRepository historyRepository,
        ILogger<FundSyncService> logger)
    {
        _profileRepository = profileRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFromFundListAsync(
        FundListSyncRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var profilesProcessed = 0;
        var historyRecords = new List<FundHistoryRecord>();

        foreach (var dto in request.Funds)
        {
            if (string.IsNullOrWhiteSpace(dto.Isin) || string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogWarning("Skipping fund with missing ISIN or Name");
                continue;
            }

            IsinId isinId;
            try
            {
                isinId = IsinId.Create(dto.Isin);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Skipping fund with invalid ISIN: {Isin}", dto.Isin);
                continue;
            }

            var profile = CreateProfile(dto, isinId, now);
            await _profileRepository.UpsertAsync(profile, cancellationToken);

            var historyRecord = CreateHistoryRecord(dto, isinId);
            historyRecords.Add(historyRecord);
            profilesProcessed++;
        }

        // Upsert history records (overwrites existing daily snapshots by ISIN+NavDate)
        await _historyRepository.UpsertRangeAsync(historyRecords, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Fund list sync: {Profiles} profiles, {History} history records",
            profilesProcessed, historyRecords.Count);

        return new FundSyncResponse
        {
            Success = true,
            Message = $"Synced {profilesProcessed} fund profiles with {historyRecords.Count} history records.",
            ProfilesProcessed = profilesProcessed,
            HistoryRecordsInserted = historyRecords.Count
        };
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFromFundAboutAsync(
        FundAboutSyncRequest request, CancellationToken cancellationToken = default)
    {
        var dto = request.Profile;

        if (string.IsNullOrWhiteSpace(dto.Isin) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return new FundSyncResponse
            {
                Success = false,
                Message = "Fund profile must have ISIN and Name."
            };
        }

        IsinId isinId;
        try
        {
            isinId = IsinId.Create(dto.Isin);
        }
        catch (ArgumentException ex)
        {
            return new FundSyncResponse
            {
                Success = false,
                Message = $"Invalid ISIN format: {ex.Message}"
            };
        }

        var now = DateTimeOffset.UtcNow;
        var profile = CreateProfile(dto, isinId, now);
        profile.AboutFundLastVisitedAt = now;
        await _profileRepository.UpsertAsync(profile, cancellationToken);

        // Build chart history records (Nav + NavDate only)
        var historyRecords = new List<FundHistoryRecord>();
        foreach (var point in request.HistoryRecords)
        {
            var navDate = ParseDateOnly(point.NavDate);
            if (navDate == null || point.Nav == null) continue;

            IsinId pointIsinId;
            try
            {
                pointIsinId = IsinId.Create(point.Isin);
            }
            catch (ArgumentException)
            {
                continue;
            }

            historyRecords.Add(new FundHistoryRecord
            {
                IsinId = pointIsinId,
                Nav = point.Nav,
                NavDate = navDate
            });
        }

        // Insert-only: skip existing (ISIN, NavDate) pairs since chart data is immutable
        await _historyRepository.InsertIfNotExistsRangeAsync(historyRecords, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Fund about sync for {Isin}: {History} history records",
            dto.Isin, historyRecords.Count);

        return new FundSyncResponse
        {
            Success = true,
            Message = $"Synced profile for {dto.Isin} with {historyRecords.Count} chart history records.",
            ProfilesProcessed = 1,
            HistoryRecordsInserted = historyRecords.Count
        };
    }

    private static FundProfile CreateProfile(ApiFundDto dto, IsinId isinId, DateTimeOffset now)
    {
        return new FundProfile
        {
            Id = isinId,
            Name = dto.Name,
            OrderbookId = dto.OrderbookId,
            Category = dto.Category,
            CompanyName = dto.CompanyName,
            FundType = dto.FundType,
            IsIndexFund = dto.IsIndexFund,
            CurrencyCode = dto.CurrencyCode,
            ManagedType = dto.ManagedType,
            StartDate = ParseDateOnly(dto.StartDate),
            Buyable = dto.Buyable,
            HasCashDividends = dto.HasCashDividends,
            HasCurrencyExchangeFee = dto.HasCurrencyExchangeFee,
            RecommendedHoldingPeriod = dto.RecommendedHoldingPeriod,
            ManagementFee = dto.ManagementFee,
            TotalFee = dto.TotalFee,
            TransactionFee = dto.TransactionFee,
            OngoingFee = dto.OngoingFee,
            MinimumBuy = dto.MinimumBuy,
            Capital = dto.Capital,
            NumberOfOwners = dto.NumberOfOwners,
            Rating = dto.Rating,
            Risk = dto.Risk,
            SharpeRatio = dto.SharpeRatio,
            StandardDeviation = dto.StandardDeviation,
            SustainabilityLevel = dto.SustainabilityLevel,
            SustainabilityRating = dto.SustainabilityRating,
            EsgScore = dto.EsgScore,
            EnvironmentalScore = dto.EnvironmentalScore,
            SocialScore = dto.SocialScore,
            GovernanceScore = dto.GovernanceScore,
            LowCarbon = dto.LowCarbon,
            EuArticleType = dto.EuArticleType,
            FirstSeenAt = now,
            CrawlerLastUpdatedAt = now
        };
    }

    private static FundHistoryRecord CreateHistoryRecord(ApiFundDto dto, IsinId isinId)
    {
        return new FundHistoryRecord
        {
            IsinId = isinId,
            Nav = dto.Nav,
            NavDate = ParseDateOnly(dto.NavDate),
            Capital = dto.Capital,
            NumberOfOwners = dto.NumberOfOwners,
            Risk = dto.Risk,
            SharpeRatio = dto.SharpeRatio,
            StandardDeviation = dto.StandardDeviation
        };
    }

    private static DateOnly? ParseDateOnly(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;
        return DateOnly.TryParse(dateString, out var date) ? date : null;
    }
}
