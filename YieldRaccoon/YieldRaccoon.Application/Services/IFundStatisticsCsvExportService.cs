namespace YieldRaccoon.Application.Services;

/// <summary>
/// Service for computing fund summary statistics from a SQLite database
/// and exporting them as a CSV file.
/// </summary>
/// <remarks>
/// <para>Reads the source database in read-only mode — nothing is modified or deleted.</para>
/// <para>
/// Slices each fund's NAV history into non-overlapping time windows (e.g., 2 weeks),
/// computing 13 summary statistics per window:
/// </para>
/// <list type="bullet">
///   <item>first_nav, last_nav, nav_high, nav_low</item>
///   <item>total_return_pct, ann_volatility, max_drawdown_pct, current_drawdown_pct</item>
///   <item>sharpe_ratio, best_day_pct, worst_day_pct, pct_positive_days, skewness</item>
/// </list>
/// </remarks>
public interface IFundStatisticsCsvExportService
{
    /// <summary>
    /// Reads fund NAV data, computes summary statistics per time window, and writes results to CSV.
    /// </summary>
    /// <param name="sourceDatabasePath">Path to the SQLite database file containing fund data.</param>
    /// <param name="csvOutputPath">Path where the CSV file will be written.</param>
    /// <param name="windowSizeDays">Size of each non-overlapping time window in calendar days (e.g., 14 for 2 weeks).</param>
    /// <param name="companyName">Optional company name filter (case-insensitive). Null or empty to include all companies.</param>
    /// <param name="minNumberOfOwners">Minimum number of owners a fund must have to be included (0 to skip filter).</param>
    /// <param name="cutoffDate">Optional earliest date for NAV data. Data before this date is excluded. Null to include all history.</param>
    /// <param name="progress">Optional progress reporter. Reports (processed fund count, total fund count).</param>
    /// <returns>The total number of rows written to the CSV file.</returns>
    Task<int> ExportAsync(
        string sourceDatabasePath,
        string csvOutputPath,
        int windowSizeDays,
        string? companyName = null,
        int minNumberOfOwners = 0,
        DateOnly? cutoffDate = null,
        IProgress<(int processed, int total)>? progress = null);
}
