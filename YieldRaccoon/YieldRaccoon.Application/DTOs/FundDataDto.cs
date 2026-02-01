namespace YieldRaccoon.Application.DTOs;

/// <summary>
/// Data transfer object for fund data received from external sources.
/// </summary>
/// <remarks>
/// <para>
/// This DTO is used at the application boundary to transfer fund data from the presentation layer
/// to the application services. It contains all properties that may be received from the API.
/// The <see cref="Services.IFundIngestionService"/> maps this DTO to domain entities
/// (<see cref="Domain.Entities.FundProfile"/> and <see cref="Domain.Entities.FundHistoryRecord"/>).
/// </para>
/// </remarks>
public sealed class FundDataDto
{
    // ===== IDENTIFIERS =====

    /// <summary>
    /// Gets or sets the fund ISIN code.
    /// </summary>
    public string? Isin { get; set; }

    /// <summary>
    /// Gets or sets the fund name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the orderbook ID.
    /// </summary>
    public string? OrderbookId { get; set; }

    // ===== METADATA =====

    /// <summary>
    /// Gets or sets the fund category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the management company name.
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Gets or sets the fund type.
    /// </summary>
    public string? FundType { get; set; }

    /// <summary>
    /// Gets or sets whether this is an index fund.
    /// </summary>
    public bool? IsIndexFund { get; set; }

    /// <summary>
    /// Gets or sets the fund start date.
    /// </summary>
    public string? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Gets or sets the management type (ACTIVE or PASSIVE).
    /// </summary>
    public string? ManagedType { get; set; }

    /// <summary>
    /// Gets or sets whether the fund is buyable.
    /// </summary>
    public bool? Buyable { get; set; }

    /// <summary>
    /// Gets or sets whether the fund has cash dividends.
    /// </summary>
    public bool? HasCashDividends { get; set; }

    /// <summary>
    /// Gets or sets the recommended holding period.
    /// </summary>
    public string? RecommendedHoldingPeriod { get; set; }

    /// <summary>
    /// Gets or sets whether there is a currency exchange fee.
    /// </summary>
    public bool? HasCurrencyExchangeFee { get; set; }

    // ===== FEES =====

    /// <summary>
    /// Gets or sets the management fee percentage.
    /// </summary>
    public decimal? ManagementFee { get; set; }

    /// <summary>
    /// Gets or sets the total fee percentage.
    /// </summary>
    public decimal? TotalFee { get; set; }

    /// <summary>
    /// Gets or sets the transaction fee percentage.
    /// </summary>
    public decimal? TransactionFee { get; set; }

    /// <summary>
    /// Gets or sets the ongoing fee percentage.
    /// </summary>
    public decimal? OngoingFee { get; set; }

    /// <summary>
    /// Gets or sets the minimum purchase amount.
    /// </summary>
    public decimal? MinimumBuy { get; set; }

    // ===== FINANCIAL DATA (TIME-VARYING) =====

    /// <summary>
    /// Gets or sets the net asset value (NAV).
    /// </summary>
    public decimal? Nav { get; set; }

    /// <summary>
    /// Gets or sets the NAV date.
    /// </summary>
    public string? NavDate { get; set; }

    /// <summary>
    /// Gets or sets the fund capital (total assets under management).
    /// </summary>
    public decimal? Capital { get; set; }

    /// <summary>
    /// Gets or sets the number of owners.
    /// </summary>
    public int? NumberOfOwners { get; set; }

    // ===== RISK METRICS (TIME-VARYING) =====

    /// <summary>
    /// Gets or sets the fund rating (1-5).
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Gets or sets the risk level (1-7).
    /// </summary>
    public int? Risk { get; set; }

    /// <summary>
    /// Gets or sets the Sharpe ratio.
    /// </summary>
    public decimal? SharpeRatio { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation.
    /// </summary>
    public decimal? StandardDeviation { get; set; }

    // ===== SUSTAINABILITY =====

    /// <summary>
    /// Gets or sets the sustainability level (WORSE, AVERAGE, BETTER).
    /// </summary>
    public string? SustainabilityLevel { get; set; }

    /// <summary>
    /// Gets or sets the sustainability rating (1-5).
    /// </summary>
    public int? SustainabilityRating { get; set; }

    /// <summary>
    /// Gets or sets the ESG score.
    /// </summary>
    public decimal? EsgScore { get; set; }

    /// <summary>
    /// Gets or sets the environmental score.
    /// </summary>
    public decimal? EnvironmentalScore { get; set; }

    /// <summary>
    /// Gets or sets the social score.
    /// </summary>
    public decimal? SocialScore { get; set; }

    /// <summary>
    /// Gets or sets the governance score.
    /// </summary>
    public decimal? GovernanceScore { get; set; }

    /// <summary>
    /// Gets or sets whether the fund is classified as low carbon.
    /// </summary>
    public bool? LowCarbon { get; set; }

    /// <summary>
    /// Gets or sets the EU article type (SFDR classification).
    /// </summary>
    public string? EuArticleType { get; set; }

    /// <summary>
    /// Returns a string representation of the fund data DTO.
    /// </summary>
    public override string ToString() => $"{Name} ({Isin})";
}
