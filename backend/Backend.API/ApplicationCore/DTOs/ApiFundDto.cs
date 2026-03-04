namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// HTTP API DTO for fund data received from YieldRaccoon.
/// Carries fund profile (static metadata) and one daily snapshot (time-varying metrics).
/// </summary>
/// <remarks>
/// Mirror of YieldRaccoon.Application.DTOs.Api.ApiFundDto — same shape, own namespace.
/// </remarks>
public sealed record ApiFundDto
{
    #region Identifiers

    public required string Isin { get; init; }
    public required string Name { get; init; }
    public string? OrderbookId { get; init; }

    #endregion

    #region Metadata

    public string? Category { get; init; }
    public string? CompanyName { get; init; }
    public string? FundType { get; init; }
    public bool? IsIndexFund { get; init; }
    public string? StartDate { get; init; }
    public string? CurrencyCode { get; init; }
    public string? ManagedType { get; init; }
    public bool? Buyable { get; init; }
    public bool? HasCashDividends { get; init; }
    public bool? HasCurrencyExchangeFee { get; init; }
    public string? RecommendedHoldingPeriod { get; init; }

    #endregion

    #region Fees

    public decimal? ManagementFee { get; init; }
    public decimal? TotalFee { get; init; }
    public decimal? TransactionFee { get; init; }
    public decimal? OngoingFee { get; init; }
    public decimal? MinimumBuy { get; init; }

    #endregion

    #region Financial Data (time-varying)

    public decimal? Nav { get; init; }
    public string? NavDate { get; init; }
    public decimal? Capital { get; init; }
    public int? NumberOfOwners { get; init; }

    #endregion

    #region Risk Metrics (time-varying)

    public int? Rating { get; init; }
    public int? Risk { get; init; }
    public decimal? SharpeRatio { get; init; }
    public decimal? StandardDeviation { get; init; }

    #endregion

    #region Sustainability

    public string? SustainabilityLevel { get; init; }
    public int? SustainabilityRating { get; init; }
    public decimal? EsgScore { get; init; }
    public decimal? EnvironmentalScore { get; init; }
    public decimal? SocialScore { get; init; }
    public decimal? GovernanceScore { get; init; }
    public bool? LowCarbon { get; init; }
    public string? EuArticleType { get; init; }

    #endregion

    #region Timestamps

    /// <summary>ISO 8601 timestamp when this fund was first discovered by the crawler.</summary>
    public string? FirstSeenAt { get; init; }

    /// <summary>ISO 8601 timestamp when the crawler last updated this fund.</summary>
    public string? CrawlerLastUpdatedAt { get; init; }

    /// <summary>ISO 8601 timestamp when the about-fund orchestrator last visited this fund.</summary>
    public string? AboutFundLastVisitedAt { get; init; }

    #endregion
}
