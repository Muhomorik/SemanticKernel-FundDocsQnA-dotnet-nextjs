using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Entities;

/// <summary>
/// Aggregate root representing static fund profile information.
/// </summary>
/// <remarks>
/// <para>
/// Contains fund metadata that rarely changes: identifiers, fees, sustainability scores.
/// Keyed by <see cref="IsinId"/> (ISIN). Time-varying data is stored in <see cref="FundHistoryRecord"/>.
/// </para>
/// </remarks>
[DebuggerDisplay("FundProfile: {Name} ({Id})")]
public sealed class FundProfile
{
    /// <summary>
    /// Fund identifier (ISIN). Primary key.
    /// </summary>
    public required IsinId Id { get; init; }

    /// <summary>
    /// Fund display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// External orderbook identifier from data source.
    /// </summary>
    public string? OrderbookId { get; set; }

    /// <summary>
    /// Fund category classification.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Fund management company name.
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Fund type (e.g., equity, bond, mixed).
    /// </summary>
    public string? FundType { get; set; }

    /// <summary>
    /// Whether this is an index-tracking fund.
    /// </summary>
    public bool? IsIndexFund { get; set; }

    /// <summary>
    /// ISO 4217 currency code (e.g., SEK, EUR, USD).
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Management type: ACTIVE or PASSIVE.
    /// </summary>
    public string? ManagedType { get; set; }

    /// <summary>
    /// Fund inception date.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Whether the fund is currently available for purchase.
    /// </summary>
    public bool? Buyable { get; set; }

    /// <summary>
    /// Whether the fund distributes cash dividends.
    /// </summary>
    public bool? HasCashDividends { get; set; }

    /// <summary>
    /// Whether currency exchange fee applies.
    /// </summary>
    public bool? HasCurrencyExchangeFee { get; set; }

    /// <summary>
    /// Recommended holding period for investors.
    /// </summary>
    public string? RecommendedHoldingPeriod { get; set; }

    /// <summary>
    /// Annual management fee as decimal (e.g., 0.0125 = 1.25%).
    /// </summary>
    public decimal? ManagementFee { get; set; }

    /// <summary>
    /// Total expense ratio as decimal.
    /// </summary>
    public decimal? TotalFee { get; set; }

    /// <summary>
    /// Transaction fee as decimal.
    /// </summary>
    public decimal? TransactionFee { get; set; }

    /// <summary>
    /// Ongoing charges as decimal.
    /// </summary>
    public decimal? OngoingFee { get; set; }

    /// <summary>
    /// Minimum purchase amount in fund currency.
    /// </summary>
    public decimal? MinimumBuy { get; set; }

    /// <summary>
    /// Total assets under management (latest snapshot).
    /// </summary>
    public decimal? Capital { get; set; }

    /// <summary>
    /// Number of unique investors holding the fund (latest snapshot).
    /// </summary>
    public int? NumberOfOwners { get; set; }

    /// <summary>
    /// Fund rating (1-5 stars).
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Risk level (1-7, SRRI/SRI indicator).
    /// </summary>
    public int? Risk { get; set; }

    /// <summary>
    /// Sharpe ratio (risk-adjusted return).
    /// </summary>
    public decimal? SharpeRatio { get; set; }

    /// <summary>
    /// Standard deviation (annualized volatility).
    /// </summary>
    public decimal? StandardDeviation { get; set; }

    /// <summary>
    /// Sustainability level: WORSE, AVERAGE, BETTER.
    /// </summary>
    public string? SustainabilityLevel { get; set; }

    /// <summary>
    /// Sustainability rating (1-5).
    /// </summary>
    public int? SustainabilityRating { get; set; }

    /// <summary>
    /// Overall ESG score.
    /// </summary>
    public decimal? EsgScore { get; set; }

    /// <summary>
    /// Environmental pillar score.
    /// </summary>
    public decimal? EnvironmentalScore { get; set; }

    /// <summary>
    /// Social pillar score.
    /// </summary>
    public decimal? SocialScore { get; set; }

    /// <summary>
    /// Governance pillar score.
    /// </summary>
    public decimal? GovernanceScore { get; set; }

    /// <summary>
    /// Whether fund is classified as low carbon.
    /// </summary>
    public bool? LowCarbon { get; set; }

    /// <summary>
    /// EU SFDR classification (Article 6, 8, or 9).
    /// </summary>
    public string? EuArticleType { get; set; }

    /// <summary>
    /// Timestamp when this fund was first discovered during crawling.
    /// </summary>
    public required DateTimeOffset FirstSeenAt { get; init; }

    /// <summary>
    /// Timestamp when fund data was last updated by the crawler.
    /// Used to skip re-crawling within a time window (e.g., same day).
    /// </summary>
    public DateTimeOffset? CrawlerLastUpdatedAt { get; set; }

    /// <summary>
    /// Timestamp when this fund was last visited by the about-fund orchestrator.
    /// Used for schedule ordering: never-visited or least-recently-visited funds are prioritized.
    /// </summary>
    public DateTimeOffset? AboutFundLastVisitedAt { get; set; }

    /// <summary>
    /// Collection of historical snapshots for this fund.
    /// </summary>
    public ICollection<FundHistoryRecord> HistoryRecords { get; init; } = new List<FundHistoryRecord>();

    /// <inheritdoc />
    public override string ToString() => $"{Name} ({Id})";
}
