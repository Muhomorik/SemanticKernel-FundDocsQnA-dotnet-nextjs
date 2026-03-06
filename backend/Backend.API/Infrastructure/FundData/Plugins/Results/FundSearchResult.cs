namespace Backend.API.Infrastructure.FundData.Plugins.Results;

/// <summary>
/// Result record returned by <see cref="FundDataPlugin.SearchFundsAsync"/>.
/// Compact fund summary for multi-criteria search results.
/// </summary>
public record FundSearchResult(
    string Isin,
    string Name,
    string? Category,
    int? Risk,
    string? ManagedType,
    int? SustainabilityRating,
    decimal? ManagementFee,
    decimal? TotalFee,
    string? EuArticleType);
