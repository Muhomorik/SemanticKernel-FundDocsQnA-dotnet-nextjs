namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// HTTP API DTO for static fund profile metadata used in the full-sync path (POST /api/funds/full-sync).
/// Contains only static/structural fields — excludes all time-varying history record fields
/// (Nav, NavDate, Capital, NumberOfOwners, Risk, SharpeRatio, StandardDeviation),
/// which travel separately in <see cref="ApiFundFullHistoryRecordDto"/>.
/// </summary>
/// <remarks>
/// Mirror of YieldRaccoon.Application.DTOs.Api.ApiFundFullSyncProfileMetadataDto — same shape, own namespace.
/// Used exclusively by <see cref="FundFullHistorySyncRequest"/>. Not interchangeable with <see cref="ApiFundDto"/>.
/// </remarks>
public sealed record ApiFundFullSyncProfileMetadataDto
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

    #region Rating (static — not time-varying Risk from history records)

    public int? Rating { get; init; }

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
