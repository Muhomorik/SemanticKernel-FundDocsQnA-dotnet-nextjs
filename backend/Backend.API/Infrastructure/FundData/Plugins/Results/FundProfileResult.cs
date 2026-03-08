namespace Backend.API.Infrastructure.FundData.Plugins.Results;

/// <summary>
/// Result record returned by <see cref="FundDataPlugin.GetFundProfileAsync"/>.
/// Contains static fund metadata for a single fund.
/// </summary>
public record FundProfileResult(
    string Isin,
    string Name,
    string? Category,
    string? CompanyName,
    string? ManagedType,
    int? Risk,
    decimal? ManagementFee,
    decimal? TotalFee,
    decimal? EsgScore,
    int? SustainabilityRating,
    string? SustainabilityLevel,
    decimal? EnvironmentalScore,
    decimal? SocialScore,
    decimal? GovernanceScore,
    string? EuArticleType,
    int? NumberOfOwners,
    decimal? Capital,
    int? Rating,
    string? CurrencyCode);
