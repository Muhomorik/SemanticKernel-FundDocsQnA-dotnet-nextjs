using System.Globalization;
using Microsoft.Data.Sqlite;
using NLog;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Reads fund NAV data from a SQLite database (read-only), computes summary statistics
/// per fund per time window, and writes results to a CSV file.
/// </summary>
public class FundStatisticsCsvExportService : IFundStatisticsCsvExportService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundStatisticsCsvExportService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FundStatisticsCsvExportService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> ExportAsync(
        string sourceDatabasePath,
        string csvOutputPath,
        int windowSizeDays,
        string? companyName = null,
        int minNumberOfOwners = 0,
        DateOnly? cutoffDate = null,
        IProgress<(int processed, int total)>? progress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvOutputPath);

        if (windowSizeDays < 1)
            throw new ArgumentOutOfRangeException(nameof(windowSizeDays), "Window size must be at least 1 day.");

        if (!File.Exists(sourceDatabasePath))
            throw new FileNotFoundException("Source database file not found.", sourceDatabasePath);

        _logger.Info("Starting CSV statistics export: source={0}, dest={1}, window={2}d, company={3}, minOwners={4}, cutoff={5}",
            sourceDatabasePath, csvOutputPath, windowSizeDays, companyName ?? "(all)", minNumberOfOwners, cutoffDate?.ToString("yyyy-MM-dd") ?? "(all)");

        var connectionString = $"Data Source={sourceDatabasePath};Mode=ReadOnly";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Step 1: Read qualifying fund profiles
        var fundProfiles = await ReadFundProfilesAsync(connection, companyName, minNumberOfOwners);
        _logger.Info("Found {0} qualifying funds", fundProfiles.Count);

        // Step 2: For each fund, read NAV series, window it, compute stats
        var allStats = new List<FundSummaryStatistics>();

        var processed = 0;
        foreach (var (isin, name) in fundProfiles)
        {
            var navSeries = await ReadNavSeriesAsync(connection, isin, cutoffDate);

            if (navSeries.Count < 2)
            {
                _logger.Debug("Skipping {0} — only {1} NAV data point(s)", isin, navSeries.Count);
            }
            else
            {
                var windows = SliceIntoWindows(navSeries, windowSizeDays);

                foreach (var window in windows)
                {
                    var navValues = window.Select(p => p.nav).ToArray();
                    var periodStart = window[0].date;
                    var periodEnd = window[^1].date;

                    var stats = FundStatisticsCalculator.Compute(isin, name, periodStart, periodEnd, navValues);
                    if (stats != null)
                        allStats.Add(stats);
                }
            }

            processed++;
            progress?.Report((processed, fundProfiles.Count));
        }

        await connection.CloseAsync();

        _logger.Info("Computed {0} statistics rows from {1} funds", allStats.Count, fundProfiles.Count);

        // Step 3: Write CSV
        var outputDir = Path.GetDirectoryName(csvOutputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        await WriteCsvAsync(csvOutputPath, allStats);

        _logger.Info("CSV export completed: {0} ({1} rows)", csvOutputPath, allStats.Count);
        return allStats.Count;
    }

    private static async Task<List<(string isin, string name)>> ReadFundProfilesAsync(
        SqliteConnection connection,
        string? companyName,
        int minNumberOfOwners)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT fp.Isin, fp.Name
            FROM FundProfiles fp
            WHERE (@company IS NULL OR LOWER(fp.CompanyName) = LOWER(@company))
              AND (fp.NumberOfOwners IS NOT NULL AND fp.NumberOfOwners >= @minOwners)
            ORDER BY fp.Isin
            """;

        command.Parameters.AddWithValue("@company",
            string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName.Trim());
        command.Parameters.AddWithValue("@minOwners", minNumberOfOwners);

        var profiles = new List<(string isin, string name)>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var isin = reader.GetString(0);
            var name = reader.IsDBNull(1) ? isin : reader.GetString(1);
            profiles.Add((isin, name));
        }

        return profiles;
    }

    private static async Task<List<(DateOnly date, decimal nav)>> ReadNavSeriesAsync(
        SqliteConnection connection,
        string isin,
        DateOnly? cutoffDate)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT NavDate, Nav
            FROM FundHistoryRecords
            WHERE FundId = @isin AND Nav IS NOT NULL AND NavDate IS NOT NULL
              AND (@cutoff IS NULL OR NavDate >= @cutoff)
            ORDER BY NavDate ASC
            """;

        command.Parameters.AddWithValue("@isin", isin);
        command.Parameters.AddWithValue("@cutoff",
            cutoffDate.HasValue ? cutoffDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);

        var series = new List<(DateOnly date, decimal nav)>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var dateStr = reader.GetString(0);
            var nav = reader.GetDecimal(1);

            if (DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                series.Add((date, nav));
        }

        return series;
    }

    /// <summary>
    /// Slices a chronologically sorted NAV series into non-overlapping windows of
    /// <paramref name="windowSizeDays"/> calendar days. The last window may be shorter.
    /// Windows with fewer than 2 data points are discarded.
    /// </summary>
    internal static List<List<(DateOnly date, decimal nav)>> SliceIntoWindows(
        List<(DateOnly date, decimal nav)> series,
        int windowSizeDays)
    {
        var windows = new List<List<(DateOnly date, decimal nav)>>();
        if (series.Count == 0)
            return windows;

        var windowStart = series[0].date;
        var currentWindow = new List<(DateOnly date, decimal nav)>();

        foreach (var point in series)
        {
            if (point.date.DayNumber - windowStart.DayNumber >= windowSizeDays)
            {
                // Current window is complete — save it and start a new one
                if (currentWindow.Count >= 2)
                    windows.Add(currentWindow);

                currentWindow = new List<(DateOnly date, decimal nav)>();
                windowStart = point.date;
            }

            currentWindow.Add(point);
        }

        // Don't forget the last window
        if (currentWindow.Count >= 2)
            windows.Add(currentWindow);

        return windows;
    }

    private static async Task WriteCsvAsync(string path, List<FundSummaryStatistics> statistics)
    {
        await using var writer = new StreamWriter(path, append: false, encoding: new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        // Header
        await writer.WriteLineAsync("series,period_start,period_end,first_nav,last_nav,nav_high,nav_low,total_return_pct,ann_volatility,max_drawdown_pct,current_drawdown_pct,sharpe_ratio,best_day_pct,worst_day_pct,pct_positive_days,skewness");

        // Data rows
        foreach (var s in statistics)
        {
            var name = EscapeCsvField(s.Name);
            var line = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12:F4},{13:F4},{14:F4},{15:F4}",
                name,
                s.PeriodStart.ToString("yyyy-MM-dd"),
                s.PeriodEnd.ToString("yyyy-MM-dd"),
                s.FirstNav,
                s.LastNav,
                s.NavHigh,
                s.NavLow,
                s.TotalReturnPct,
                s.AnnVolatility,
                s.MaxDrawdownPct,
                s.CurrentDrawdownPct,
                s.SharpeRatio,
                s.BestDayPct,
                s.WorstDayPct,
                s.PctPositiveDays,
                s.Skewness);

            await writer.WriteLineAsync(line);
        }
    }

    /// <summary>
    /// Escapes a CSV field per RFC 4180: wraps in double quotes if it contains comma, quote, or newline.
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            return $"\"{field.Replace("\"", "\"\"")}\"";

        return field;
    }
}
