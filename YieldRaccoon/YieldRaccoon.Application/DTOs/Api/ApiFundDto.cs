namespace YieldRaccoon.Application.DTOs.Api;

/// <summary>
/// HTTP API DTO for fund data sent to the Backend API.
/// Carries fund profile (static metadata) and one daily snapshot (time-varying metrics).
/// </summary>
/// <remarks>
/// <para>
/// This is the wire format for the <c>/api/funds/list</c> and <c>/api/funds/about</c> endpoints.
/// The Backend API has its own identical DTO — no project reference between them.
/// </para>
/// <para>
/// All properties use JSON-friendly types (string for dates, nullable for optional fields).
/// The Backend is responsible for parsing and validating values.
/// </para>
/// </remarks>
public sealed record ApiFundDto
{
    // ===== IDENTIFIERS =====

    public required string Isin { get; init; }
    public required string Name { get; init; }
    public string? OrderbookId { get; init; }

    // ===== METADATA =====

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

    // ===== FEES =====

    public decimal? ManagementFee { get; init; }
    public decimal? TotalFee { get; init; }
    public decimal? TransactionFee { get; init; }
    public decimal? OngoingFee { get; init; }
    public decimal? MinimumBuy { get; init; }

    // ===== FINANCIAL DATA (TIME-VARYING) =====

    public decimal? Nav { get; init; }
    public string? NavDate { get; init; }
    public decimal? Capital { get; init; }
    public int? NumberOfOwners { get; init; }

    // ===== RISK METRICS (TIME-VARYING) =====

    public int? Rating { get; init; }
    public int? Risk { get; init; }
    public decimal? SharpeRatio { get; init; }
    public decimal? StandardDeviation { get; init; }

    // ===== SUSTAINABILITY =====

    public string? SustainabilityLevel { get; init; }
    public int? SustainabilityRating { get; init; }
    public decimal? EsgScore { get; init; }
    public decimal? EnvironmentalScore { get; init; }
    public decimal? SocialScore { get; init; }
    public decimal? GovernanceScore { get; init; }
    public bool? LowCarbon { get; init; }
    public string? EuArticleType { get; init; }
}
