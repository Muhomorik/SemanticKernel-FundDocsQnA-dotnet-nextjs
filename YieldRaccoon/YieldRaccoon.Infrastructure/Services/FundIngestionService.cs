using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Service for ingesting fund data into the persistence layer.
/// </summary>
/// <remarks>
/// <para>
/// This service orchestrates the mapping of <see cref="FundDataDto"/> to domain entities:
/// <list type="bullet">
///     <item><see cref="FundProfile"/> - Static fund information (upserted)</item>
///     <item><see cref="FundHistoryRecord"/> - Time-varying data (appended if not duplicate)</item>
/// </list>
/// </para>
/// </remarks>
public class FundIngestionService : IFundIngestionService
{
    private readonly IFundProfileRepository _profileRepository;
    private readonly IFundHistoryRepository _historyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundIngestionService"/> class.
    /// </summary>
    /// <param name="profileRepository">The fund profile repository.</param>
    /// <param name="historyRepository">The fund history repository.</param>
    public FundIngestionService(
        IFundProfileRepository profileRepository,
        IFundHistoryRepository historyRepository)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
    }

    /// <inheritdoc />
    public async Task<int> IngestBatchAsync(
        IEnumerable<FundDataDto> fundDataList,
        CancellationToken cancellationToken = default)
    {
        var successCount = 0;
        var now = DateTimeOffset.UtcNow;
        var historyRecords = new List<FundHistoryRecord>();

        foreach (var fundData in fundDataList)
        {
            if (string.IsNullOrWhiteSpace(fundData.Isin) || string.IsNullOrWhiteSpace(fundData.Name))
            {
                continue;
            }

            var fundId = IsinId.Create(fundData.Isin);

            // AddOrUpdate profile (repository handles the logic)
            var profile = CreateProfile(fundData, fundId, now);
            await _profileRepository.AddOrUpdateAsync(profile, cancellationToken);

            // Create history record
            var historyRecord = CreateHistoryRecord(fundData, fundId);
            historyRecords.Add(historyRecord);
            successCount++;
        }

        // Add or update history records (repository handles duplicate detection by FundId + NavDate)
        await _historyRepository.AddOrUpdateRangeAsync(historyRecords, cancellationToken);

        // Save all changes
        await _profileRepository.SaveChangesAsync(cancellationToken);
        await _historyRepository.SaveChangesAsync(cancellationToken);

        return successCount;
    }

    private static FundProfile CreateProfile(FundDataDto dto, IsinId isinId, DateTimeOffset now)
    {
        return new FundProfile
        {
            Id = isinId,
            Name = dto.Name!,
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

    private static FundHistoryRecord CreateHistoryRecord(FundDataDto dto, IsinId isinId)
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

    // Parses a date string to DateOnly, returning null if parsing fails
    private static DateOnly? ParseDateOnly(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        return DateOnly.TryParse(dateString, out var date) ? date : null;
    }
}
