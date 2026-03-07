namespace Backend.API.Infrastructure.FundData.Plugins.Results;

/// <summary>
/// Result record returned by <see cref="FundDataPlugin.GetCategoryPerformanceAsync"/>.
/// Shows average NAV performance across all funds in a category.
/// </summary>
public record CategoryPerformanceResult(
    string Category,
    decimal AveragePercentChange,
    int FundCount);
