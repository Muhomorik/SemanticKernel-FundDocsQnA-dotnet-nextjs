using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Domain.Entities;

namespace YieldRaccoon.Infrastructure.Mappers;

/// <summary>
/// Maps between application DTOs / domain entities and the Backend API wire format.
/// </summary>
public static class ApiFundDtoMapper
{
    /// <summary>
    /// Converts a <see cref="FundDataDto"/> (crawl source) to <see cref="ApiFundDto"/> (API wire format).
    /// </summary>
    /// <remarks>
    /// Caller must ensure <see cref="FundDataDto.Isin"/> and <see cref="FundDataDto.Name"/> are non-null.
    /// </remarks>
    public static ApiFundDto ToApiFundDto(this FundDataDto dto) => new()
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
    public static ApiFundDto ToApiFundDto(this FundProfile profile) => new()
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
    };

    /// <summary>
    /// Converts a collection of <see cref="FundDataDto"/> to API DTOs,
    /// filtering out entries without valid ISIN or Name.
    /// </summary>
    public static IReadOnlyList<ApiFundDto> ToApiFundDtos(this IEnumerable<FundDataDto> dtos) =>
        dtos
            .Where(d => !string.IsNullOrWhiteSpace(d.Isin) && !string.IsNullOrWhiteSpace(d.Name))
            .Select(d => d.ToApiFundDto())
            .ToList();
}
