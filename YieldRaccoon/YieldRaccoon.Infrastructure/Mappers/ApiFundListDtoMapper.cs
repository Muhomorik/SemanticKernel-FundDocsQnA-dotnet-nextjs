using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Domain.Entities;

namespace YieldRaccoon.Infrastructure.Mappers;

/// <summary>
/// Maps between application DTOs / domain entities and the Backend API wire format.
/// </summary>
public static class ApiFundListDtoMapper
{
    /// <summary>
    /// Converts a <see cref="FundListDataDto"/> (crawl source) to <see cref="ApiFundDto"/> (API wire format).
    /// </summary>
    /// <remarks>
    /// Caller must ensure <see cref="FundListDataDto.Isin"/> and <see cref="FundListDataDto.Name"/> are non-null.
    /// </remarks>
    public static ApiFundDto ToApiFundListDto(this FundListDataDto dto) => new()
    {
        Isin = dto.Isin!,
        Name = dto.Name!,
        OrderbookId = dto.OrderbookId,
        Category = dto.Category,
        CompanyName = dto.CompanyName,
        FundType = dto.FundType,
        IsIndexFund = dto.IsIndexFund,
        StartDate = dto.StartDate,
        CurrencyCode = dto.CurrencyCode,
        ManagedType = dto.ManagedType,
        Buyable = dto.Buyable,
        HasCashDividends = dto.HasCashDividends,
        HasCurrencyExchangeFee = dto.HasCurrencyExchangeFee,
        RecommendedHoldingPeriod = dto.RecommendedHoldingPeriod,
        ManagementFee = dto.ManagementFee,
        TotalFee = dto.TotalFee,
        TransactionFee = dto.TransactionFee,
        OngoingFee = dto.OngoingFee,
        MinimumBuy = dto.MinimumBuy,
        Nav = dto.Nav,
        NavDate = dto.NavDate,
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
    };

    /// <summary>
    /// Converts a <see cref="FundProfile"/> domain entity to <see cref="ApiFundDto"/>.
    /// Used by the about-fund DualWrite decorator to include the profile in the sync request.
    /// </summary>
    public static ApiFundDto ToApiFundListDto(this FundProfile profile) => new()
    {
        Isin = profile.Id.Isin,
        Name = profile.Name,
        OrderbookId = profile.OrderbookId,
        Category = profile.Category,
        CompanyName = profile.CompanyName,
        FundType = profile.FundType,
        IsIndexFund = profile.IsIndexFund,
        StartDate = profile.StartDate?.ToString("yyyy-MM-dd"),
        CurrencyCode = profile.CurrencyCode,
        ManagedType = profile.ManagedType,
        Buyable = profile.Buyable,
        HasCashDividends = profile.HasCashDividends,
        HasCurrencyExchangeFee = profile.HasCurrencyExchangeFee,
        RecommendedHoldingPeriod = profile.RecommendedHoldingPeriod,
        Description = profile.Description,
        ManagementFee = profile.ManagementFee,
        TotalFee = profile.TotalFee,
        TransactionFee = profile.TransactionFee,
        OngoingFee = profile.OngoingFee,
        MinimumBuy = profile.MinimumBuy,
        Capital = profile.Capital,
        NumberOfOwners = profile.NumberOfOwners,
        Rating = profile.Rating,
        Risk = profile.Risk,
        SharpeRatio = profile.SharpeRatio,
        StandardDeviation = profile.StandardDeviation,
        SustainabilityLevel = profile.SustainabilityLevel,
        SustainabilityRating = profile.SustainabilityRating,
        EsgScore = profile.EsgScore,
        EnvironmentalScore = profile.EnvironmentalScore,
        SocialScore = profile.SocialScore,
        GovernanceScore = profile.GovernanceScore,
        LowCarbon = profile.LowCarbon,
        EuArticleType = profile.EuArticleType,
        FirstSeenAt = profile.FirstSeenAt.ToString("O"),
        CrawlerLastUpdatedAt = profile.CrawlerLastUpdatedAt?.ToString("O"),
        AboutFundLastVisitedAt = profile.AboutFundLastVisitedAt?.ToString("O"),
    };

    /// <summary>
    /// Converts a collection of <see cref="FundListDataDto"/> to API DTOs,
    /// filtering out entries without valid ISIN or Name.
    /// </summary>
    public static IReadOnlyList<ApiFundDto> ToApiFundListDtos(this IEnumerable<FundListDataDto> dtos) =>
        dtos
            .Where(d => !string.IsNullOrWhiteSpace(d.Isin) && !string.IsNullOrWhiteSpace(d.Name))
            .Select(d => d.ToApiFundListDto())
            .ToList();

    /// <summary>
    /// Converts a <see cref="FundProfile"/> to <see cref="ApiFundFullSyncProfileMetadataDto"/>.
    /// Excludes time-varying history record fields (Capital, NumberOfOwners, Risk, SharpeRatio, StandardDeviation).
    /// Used exclusively by the CloudSync full-history sync path.
    /// </summary>
    public static ApiFundFullSyncProfileMetadataDto ToApiFundFullSyncProfileMetadataDto(this FundProfile profile) => new()
    {
        Isin = profile.Id.Isin,
        Name = profile.Name,
        OrderbookId = profile.OrderbookId,
        Category = profile.Category,
        CompanyName = profile.CompanyName,
        FundType = profile.FundType,
        IsIndexFund = profile.IsIndexFund,
        StartDate = profile.StartDate?.ToString("yyyy-MM-dd"),
        CurrencyCode = profile.CurrencyCode,
        ManagedType = profile.ManagedType,
        Buyable = profile.Buyable,
        HasCashDividends = profile.HasCashDividends,
        HasCurrencyExchangeFee = profile.HasCurrencyExchangeFee,
        RecommendedHoldingPeriod = profile.RecommendedHoldingPeriod,
        Description = profile.Description,
        ManagementFee = profile.ManagementFee,
        TotalFee = profile.TotalFee,
        TransactionFee = profile.TransactionFee,
        OngoingFee = profile.OngoingFee,
        MinimumBuy = profile.MinimumBuy,
        Rating = profile.Rating,
        SustainabilityLevel = profile.SustainabilityLevel,
        SustainabilityRating = profile.SustainabilityRating,
        EsgScore = profile.EsgScore,
        EnvironmentalScore = profile.EnvironmentalScore,
        SocialScore = profile.SocialScore,
        GovernanceScore = profile.GovernanceScore,
        LowCarbon = profile.LowCarbon,
        EuArticleType = profile.EuArticleType,
        FirstSeenAt = profile.FirstSeenAt.ToString("O"),
        CrawlerLastUpdatedAt = profile.CrawlerLastUpdatedAt?.ToString("O"),
        AboutFundLastVisitedAt = profile.AboutFundLastVisitedAt?.ToString("O"),
    };

    /// <summary>
    /// Converts a <see cref="FundHistoryRecord"/> to <see cref="ApiFundFullHistoryRecordDto"/>.
    /// Includes all time-varying fields. Used by the CloudSync full-history sync path.
    /// </summary>
    public static ApiFundFullHistoryRecordDto ToApiFundFullHistoryRecordDto(this FundHistoryRecord record) => new()
    {
        Isin = record.IsinId.Isin,
        NavDate = record.NavDate?.ToString("yyyy-MM-dd"),
        Nav = record.Nav,
        Capital = record.Capital,
        NumberOfOwners = record.NumberOfOwners,
        Risk = record.Risk,
        SharpeRatio = record.SharpeRatio,
        StandardDeviation = record.StandardDeviation,
    };
}
