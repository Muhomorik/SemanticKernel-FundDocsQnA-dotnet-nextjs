using System.Globalization;
using Microsoft.Data.Sqlite;
using NLog;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Reads fund NAV data from a SQLite database (read-only), computes per-bucket summary statistics
/// over non-overlapping time windows, and writes the results to a CSV file.
/// </summary>
public class FundStatisticsCsvExportService : IFundStatisticsCsvExportService
{
    private const string CsvHeader =
        "isin,name,period_start,period_end,first_nav,last_nav,nav_high,nav_low," +
        "return_2w_pct,ann_volatility_2w_pct,max_drawdown_2w_pct,current_drawdown_pct,sharpe_2w," +
        "best_day_pct,worst_day_pct,pct_positive_days,skewness";

    /// <summary>
    /// Minimum span (in days) for a window to be emitted. Trailing windows narrower than this are
    /// dropped per <c>summary-csv-plan.md §7.1</c> — partial buckets with NaN-prone aggregates
    /// would otherwise contaminate downstream "X of N positive windows" counting.
    /// </summary>
    private const int MinimumWindowDays = 7;

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
        var fundProfiles = await FundQueryHelpers.ReadFundProfilesAsync(connection, companyName, minNumberOfOwners);
        _logger.Info("Found {0} qualifying funds", fundProfiles.Count);

        // Step 2: For each fund, read NAV series, window it, compute stats
        var allStats = new List<FundSummaryStatistics>();

        var processed = 0;
        foreach (var (isin, name) in fundProfiles)
        {
            var navSeries = await FundQueryHelpers.ReadNavSeriesAsync(connection, isin, cutoffDate);

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

    /// <summary>
    /// Slices a chronologically sorted NAV series into non-overlapping windows of
    /// <paramref name="windowSizeDays"/> calendar days. Drops windows narrower than
    /// <see cref="MinimumWindowDays"/> per <c>summary-csv-plan.md §7.1</c> — including the
    /// last window when the producer is run mid-cycle.
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
                if (IsWindowEmittable(currentWindow))
                    windows.Add(currentWindow);

                currentWindow = new List<(DateOnly date, decimal nav)>();
                windowStart = point.date;
            }

            currentWindow.Add(point);
        }

        if (IsWindowEmittable(currentWindow))
            windows.Add(currentWindow);

        return windows;
    }

    private static bool IsWindowEmittable(List<(DateOnly date, decimal nav)> window)
    {
        if (window.Count < 2)
            return false;

        var span = window[^1].date.DayNumber - window[0].date.DayNumber;
        return span >= MinimumWindowDays;
    }

    private static async Task WriteCsvAsync(string path, List<FundSummaryStatistics> statistics)
    {
        await using var writer = new StreamWriter(path, append: false, encoding: new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await writer.WriteLineAsync(CsvHeader);

        var seenKeys = new HashSet<(string, DateOnly)>();
        foreach (var s in statistics)
        {
            if (!seenKeys.Add((s.Isin, s.PeriodStart)))
                throw new InvalidOperationException(
                    $"Duplicate (Isin, PeriodStart) pair '{s.Isin}'/{s.PeriodStart:yyyy-MM-dd}' in summary output.");

            var name = EscapeCsvField(s.Name);
            var line = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}",
                s.Isin,
                name,
                s.PeriodStart.ToString("yyyy-MM-dd"),
                s.PeriodEnd.ToString("yyyy-MM-dd"),
                s.FirstNav.ToString(CultureInfo.InvariantCulture),
                s.LastNav.ToString(CultureInfo.InvariantCulture),
                s.NavHigh.ToString(CultureInfo.InvariantCulture),
                s.NavLow.ToString(CultureInfo.InvariantCulture),
                FormatMetric(s.Return2wPct),
                FormatMetric(s.AnnVolatility2wPct),
                FormatMetric(s.MaxDrawdown2wPct),
                FormatMetric(s.CurrentDrawdownPct),
                FormatMetric(s.Sharpe2w),
                FormatMetric(s.BestDayPct),
                FormatMetric(s.WorstDayPct),
                FormatMetric(s.PctPositiveDays),
                FormatMetric(s.Skewness));

            await writer.WriteLineAsync(line);
        }
    }

    private static string FormatMetric(double value)
    {
        return double.IsNaN(value)
            ? "NaN"
            : value.ToString("F4", CultureInfo.InvariantCulture);
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
