using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.Mappers;

/// <summary>
/// Extension methods for converting <see cref="InterceptedFund"/> to <see cref="FundDataDto"/>.
/// </summary>
public static class FundDataDtoMapper
{
    /// <summary>
    /// Converts an <see cref="InterceptedFund"/> to a <see cref="FundDataDto"/>.
    /// </summary>
    /// <param name="fund">The intercepted fund from the WebView2 API response.</param>
    /// <returns>A DTO suitable for the application layer.</returns>
    public static FundDataDto ToFundDataDto(this InterceptedFund fund)
    {
        return new FundDataDto
        {
            // Identifiers
            Isin = fund.Isin,
            Name = fund.Name,
            OrderbookId = fund.OrderbookId,

            // Metadata
            Category = fund.Category,
            CompanyName = fund.CompanyName,
            FundType = fund.FundType,
            IsIndexFund = fund.IndexFund,
            StartDate = fund.StartDate,
            CurrencyCode = fund.CurrencyCode,
            ManagedType = fund.ManagedType,
            Buyable = fund.Buyable,
            HasCashDividends = fund.HasCashDividends,
            RecommendedHoldingPeriod = fund.RecommendedHoldingPeriodValue,
            HasCurrencyExchangeFee = fund.HasCurrencyExchangeFee,

            // Fees
            ManagementFee = fund.ManagementFee,
            TotalFee = fund.TotalFee,
            TransactionFee = fund.TransactionFee,
            OngoingFee = fund.OngoingFee,
            MinimumBuy = fund.MinimumBuy,

            // Financial Data (time-varying)
            Nav = fund.Nav,
            NavDate = fund.NavDate,
            Capital = fund.Capital,
            NumberOfOwners = fund.NumberOfOwners,

            // Risk Metrics (time-varying)
            Rating = fund.Rating,
            Risk = fund.Risk,
            SharpeRatio = fund.SharpeRatio,
            StandardDeviation = fund.StandardDeviation,

            // Sustainability
            SustainabilityLevel = fund.SustainabilityLevel,
            SustainabilityRating = fund.SustainabilityRating,
            EsgScore = fund.EsgScore,
            EnvironmentalScore = fund.EnvironmentalScore,
            SocialScore = fund.SocialScore,
            GovernanceScore = fund.GovernanceScore,
            LowCarbon = fund.LowCarbon,
            EuArticleType = fund.EuArticleType?.Name
        };
    }

    /// <summary>
    /// Converts a collection of <see cref="InterceptedFund"/> to <see cref="FundDataDto"/> collection.
    /// </summary>
    /// <param name="funds">The intercepted funds.</param>
    /// <returns>A read-only collection of DTOs.</returns>
    public static IReadOnlyCollection<FundDataDto> ToFundDataDtos(this IEnumerable<InterceptedFund> funds)
    {
        return funds.Select(f => f.ToFundDataDto()).ToList();
    }
}
