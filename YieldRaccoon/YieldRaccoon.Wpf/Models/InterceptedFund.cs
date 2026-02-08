using System.Text.Json.Serialization;

namespace YieldRaccoon.Wpf.Models;

/// <summary>
/// Represents information about a single fund intercepted from API responses.
/// </summary>
public class InterceptedFund
{
    // ===== IDENTIFIERS =====

    /// <summary>
    /// Gets or sets the fund ISIN code.
    /// </summary>
    [JsonPropertyName("isin")]
    public string? Isin { get; set; }

    /// <summary>
    /// Gets or sets the fund name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the OrderBook ID.
    /// </summary>
    [JsonPropertyName("orderbookId")]
    public string? OrderBookId { get; set; }

    // ===== RISK METRICS =====

    /// <summary>
    /// Gets or sets the fund rating (1-5).
    /// </summary>
    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    /// <summary>
    /// Gets or sets the risk level (1-7).
    /// </summary>
    [JsonPropertyName("risk")]
    public int? Risk { get; set; }

    /// <summary>
    /// Gets or sets the Sharpe ratio.
    /// </summary>
    [JsonPropertyName("sharpeRatio")]
    public decimal? SharpeRatio { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation.
    /// </summary>
    [JsonPropertyName("standardDeviation")]
    public decimal? StandardDeviation { get; set; }

    // ===== FINANCIAL DATA =====

    /// <summary>
    /// Gets or sets the management fee percentage.
    /// </summary>
    [JsonPropertyName("managementFee")]
    public decimal? ManagementFee { get; set; }

    /// <summary>
    /// Gets or sets the total fee percentage.
    /// </summary>
    [JsonPropertyName("totalFee")]
    public decimal? TotalFee { get; set; }

    /// <summary>
    /// Gets or sets the transaction fee percentage.
    /// </summary>
    [JsonPropertyName("transactionFee")]
    public decimal? TransactionFee { get; set; }

    /// <summary>
    /// Gets or sets the ongoing fee percentage.
    /// </summary>
    [JsonPropertyName("ongoingFee")]
    public decimal? OngoingFee { get; set; }

    /// <summary>
    /// Gets or sets other fees.
    /// </summary>
    [JsonPropertyName("otherFee")]
    public decimal? OtherFee { get; set; }

    /// <summary>
    /// Gets or sets the minimum purchase amount.
    /// </summary>
    [JsonPropertyName("minimumBuy")]
    public decimal? MinimumBuy { get; set; }

    /// <summary>
    /// Gets or sets the minimum monthly savings amount.
    /// </summary>
    [JsonPropertyName("minimumBuyMonthlySaving")]
    public decimal? MinimumBuyMonthlySaving { get; set; }

    /// <summary>
    /// Gets or sets the fund capital (total assets under management).
    /// </summary>
    [JsonPropertyName("capital")]
    public decimal? Capital { get; set; }

    /// <summary>
    /// Gets or sets the net asset value (NAV).
    /// </summary>
    [JsonPropertyName("nav")]
    public decimal? Nav { get; set; }

    /// <summary>
    /// Gets or sets the NAV date.
    /// </summary>
    /// <example>2026-02-04T00:00:00</example>
    [JsonPropertyName("navDate")]
    public string? NavDate { get; set; }

    /// <summary>
    /// Gets or sets whether there is a currency exchange fee.
    /// </summary>
    [JsonPropertyName("hasCurrencyExchangeFee")]
    public bool? HasCurrencyExchangeFee { get; set; }

    // ===== METADATA =====

    /// <summary>
    /// Gets or sets the fund category.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the management company name.
    /// </summary>
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Gets or sets the fund type.
    /// </summary>
    [JsonPropertyName("fundType")]
    public string? FundType { get; set; }

    /// <summary>
    /// Gets or sets whether this is an index fund.
    /// </summary>
    [JsonPropertyName("indexFund")]
    public bool? IndexFund { get; set; }

    /// <summary>
    /// Gets or sets the fund start date.
    /// </summary>
    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Gets or sets the management type (ACTIVE or PASSIVE).
    /// </summary>
    [JsonPropertyName("managedType")]
    public string? ManagedType { get; set; }

    /// <summary>
    /// Gets or sets whether the fund is buyable.
    /// </summary>
    [JsonPropertyName("buyable")]
    public bool? Buyable { get; set; }

    /// <summary>
    /// Gets or sets whether the fund has cash dividends.
    /// </summary>
    [JsonPropertyName("hasCashDividends")]
    public bool? HasCashDividends { get; set; }

    /// <summary>
    /// Gets or sets the recommended holding period value.
    /// </summary>
    [JsonPropertyName("recommendedHoldingPeriodValue")]
    public string? RecommendedHoldingPeriodValue { get; set; }

    /// <summary>
    /// Gets or sets the number of owners.
    /// </summary>
    [JsonPropertyName("nrOfOwners")]
    public int? NumberOfOwners { get; set; }

    // ===== SUSTAINABILITY =====

    /// <summary>
    /// Gets or sets the sustainability level (WORSE, AVERAGE, BETTER).
    /// </summary>
    [JsonPropertyName("sustainabilityLevel")]
    public string? SustainabilityLevel { get; set; }

    /// <summary>
    /// Gets or sets the sustainability rating (1-5).
    /// </summary>
    [JsonPropertyName("sustainabilityRating")]
    public int? SustainabilityRating { get; set; }

    /// <summary>
    /// Gets or sets the sustainability rating category name.
    /// </summary>
    [JsonPropertyName("sustainabilityRatingCategoryName")]
    public string? SustainabilityRatingCategoryName { get; set; }

    /// <summary>
    /// Gets or sets the ESG score.
    /// </summary>
    [JsonPropertyName("esgScore")]
    public decimal? EsgScore { get; set; }

    /// <summary>
    /// Gets or sets the environmental score.
    /// </summary>
    [JsonPropertyName("environmentalScore")]
    public decimal? EnvironmentalScore { get; set; }

    /// <summary>
    /// Gets or sets the social score.
    /// </summary>
    [JsonPropertyName("socialScore")]
    public decimal? SocialScore { get; set; }

    /// <summary>
    /// Gets or sets the governance score.
    /// </summary>
    [JsonPropertyName("governanceScore")]
    public decimal? GovernanceScore { get; set; }

    /// <summary>
    /// Gets or sets the environmental rating (1-5).
    /// </summary>
    [JsonPropertyName("environmentalRating")]
    public int? EnvironmentalRating { get; set; }

    /// <summary>
    /// Gets or sets the social rating (1-5).
    /// </summary>
    [JsonPropertyName("socialRating")]
    public int? SocialRating { get; set; }

    /// <summary>
    /// Gets or sets the governance rating (1-5).
    /// </summary>
    [JsonPropertyName("governanceRating")]
    public int? GovernanceRating { get; set; }

    /// <summary>
    /// Gets or sets the low carbon indicator.
    /// </summary>
    [JsonPropertyName("lowCarbon")]
    public bool? LowCarbon { get; set; }

    /// <summary>
    /// Gets or sets the fossil fuel involvement percentage.
    /// </summary>
    [JsonPropertyName("fossilFuelInvolvement")]
    public decimal? FossilFuelInvolvement { get; set; }

    /// <summary>
    /// Gets or sets the carbon risk score.
    /// </summary>
    [JsonPropertyName("carbonRiskScore")]
    public decimal? CarbonRiskScore { get; set; }

    /// <summary>
    /// Gets or sets the carbon solutions involvement percentage.
    /// </summary>
    [JsonPropertyName("carbonSolutionsInvolvement")]
    public decimal? CarbonSolutionsInvolvement { get; set; }

    /// <summary>
    /// Gets or sets the AUM covered by carbon data.
    /// </summary>
    [JsonPropertyName("aumCoveredCarbon")]
    public decimal? AumCoveredCarbon { get; set; }

    /// <summary>
    /// Gets or sets the thermal coal involvement percentage.
    /// </summary>
    [JsonPropertyName("thermalCoalInvolvement")]
    public decimal? ThermalCoalInvolvement { get; set; }

    /// <summary>
    /// Gets or sets the oil sands extraction involvement percentage.
    /// </summary>
    [JsonPropertyName("oilSandsExtractionInvolvement")]
    public decimal? OilSandsExtractionInvolvement { get; set; }

    /// <summary>
    /// Gets or sets the arctic oil and gas exploration involvement percentage.
    /// </summary>
    [JsonPropertyName("arcticOilAndGasExplorationInvolvement")]
    public decimal? ArcticOilAndGasExplorationInvolvement { get; set; }

    /// <summary>
    /// Gets or sets the list of controversial product involvements.
    /// </summary>
    [JsonPropertyName("productInvolvements")]
    public List<string>? ProductInvolvements { get; set; }

    /// <summary>
    /// Gets or sets the sustainable development goals alignments.
    /// </summary>
    [JsonPropertyName("sustainableDevelopmentGoalsAlignments")]
    public List<string>? SustainableDevelopmentGoalsAlignments { get; set; }

    /// <summary>
    /// Gets or sets the EU article type (SFDR classification).
    /// </summary>
    [JsonPropertyName("euArticleType")]
    public EuArticleType? EuArticleType { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as a dictionary for flexible parsing.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }

    /// <summary>
    /// Returns a string representation of the fund info.
    /// </summary>
    public override string ToString()
    {
        return $"{Name} ({Isin}) - {CompanyName}";
    }
}

/// <summary>
/// Represents the EU article type for SFDR classification.
/// </summary>
public class EuArticleType
{
    /// <summary>
    /// Gets or sets the article name (e.g., "Artikel 6", "Artikel 8").
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the article value (e.g., "ARTICLE_TYPE_SIX", "ARTICLE_TYPE_EIGHT").
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}