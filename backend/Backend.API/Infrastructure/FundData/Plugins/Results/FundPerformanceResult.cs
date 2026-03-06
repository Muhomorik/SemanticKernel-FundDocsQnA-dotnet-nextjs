namespace Backend.API.Infrastructure.FundData.Plugins.Results;

/// <summary>
/// Result record returned by <see cref="FundDataPlugin.GetTopPerformingFundsAsync"/>.
/// Shows NAV performance over a time window for a single fund.
/// </summary>
public record FundPerformanceResult(
    string Isin,
    string Name,
    string? Category,
    decimal StartNav,
    decimal EndNav,
    decimal PercentChange,
    DateOnly StartDate,
    DateOnly EndDate);
