using System.Globalization;
using Microsoft.Data.Sqlite;
using NLog;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Reads fund NAV data from a SQLite database (read-only), computes 12-week and 1-year rolling-horizon
/// statistics anchored at the most recent NAV date, and writes one row per fund to a CSV file.
/// </summary>
public class FundSnapshotCsvExportService : IFundSnapshotCsvExportService
{
    private const string CsvHeader =
        "isin,as_of_date," +
        "return_12w_compound_pct,ann_volatility_12w_pct,sharpe_12w,max_drawdown_12w_pct," +
        "return_1y_compound_pct,ann_volatility_1y_pct,sharpe_1y,max_drawdown_1y_pct";

    private const int Horizon12wDays = 84;
    private const int Horizon1yDays = 365;

    /// <summary>
    /// Tolerance applied to the insufficient-history check — a fund is considered to have sufficient
    /// history for a horizon if its earliest NAV in the slice is no more than this many days inside
    /// the window edge. Accounts for weekends / market holidays at the start of the lookback range.
    /// </summary>
    private const int InsufficientHistoryToleranceDays = 3;

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundSnapshotCsvExportService"/> class.
    /// </summary>
    public FundSnapshotCsvExportService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> ExportAsync(
        string sourceDatabasePath,
        string csvOutputPath,
        string? companyName = null,
        int minNumberOfOwners = 0,
        IProgress<(int processed, int total)>? progress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvOutputPath);

        if (!File.Exists(sourceDatabasePath))
            throw new FileNotFoundException("Source database file not found.", sourceDatabasePath);

        _logger.Info("Starting snapshot CSV export: source={0}, dest={1}, company={2}, minOwners={3}",
            sourceDatabasePath, csvOutputPath, companyName ?? "(all)", minNumberOfOwners);

        var connectionString = $"Data Source={sourceDatabasePath};Mode=ReadOnly";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var asOfDate = await FundQueryHelpers.GetLatestNavDateAsync(connection);
        if (asOfDate is null)
        {
            _logger.Warn("No NAV rows in source database — writing header-only snapshot CSV");
            await WriteHeaderOnlyAsync(csvOutputPath);
            return 0;
        }

        var fundProfiles = await FundQueryHelpers.ReadFundProfilesAsync(connection, companyName, minNumberOfOwners);
        _logger.Info("Found {0} qualifying funds; as_of_date = {1:yyyy-MM-dd}", fundProfiles.Count, asOfDate.Value);

        var snapshots = new List<FundSnapshotStatistics>(fundProfiles.Count);
        var asOfValue = asOfDate.Value;
        var earliestNeeded = asOfValue.AddDays(-Horizon1yDays);

        var processed = 0;
        foreach (var (isin, _) in fundProfiles)
        {
            var navSeries = await FundQueryHelpers.ReadNavSeriesAsync(connection, isin, earliestNeeded);

            var slice12w = TakeHorizonSlice(navSeries, asOfValue, Horizon12wDays);
            var slice1y = TakeHorizonSlice(navSeries, asOfValue, Horizon1yDays);

            snapshots.Add(FundSnapshotStatisticsCalculator.Compute(isin, asOfValue, slice12w, slice1y));

            processed++;
            progress?.Report((processed, fundProfiles.Count));
        }

        await connection.CloseAsync();

        var outputDir = Path.GetDirectoryName(csvOutputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        await WriteCsvAsync(csvOutputPath, snapshots);

        _logger.Info("Snapshot CSV export completed: {0} ({1} rows)", csvOutputPath, snapshots.Count);
        return snapshots.Count;
    }

    /// <summary>
    /// Returns the NAV points within the trailing <paramref name="horizonDays"/> calendar days ending
    /// at <paramref name="asOfDate"/>. Returns an empty list when the fund's earliest record falls
    /// inside the window by more than <see cref="InsufficientHistoryToleranceDays"/> days, signaling
    /// "insufficient history" — the calculator emits NaN for that horizon.
    /// </summary>
    private static IReadOnlyList<(DateOnly date, decimal nav)> TakeHorizonSlice(
        IReadOnlyList<(DateOnly date, decimal nav)> navSeries,
        DateOnly asOfDate,
        int horizonDays)
    {
        if (navSeries.Count == 0)
            return [];

        var windowStart = asOfDate.AddDays(-horizonDays);
        var insufficientCutoff = windowStart.AddDays(InsufficientHistoryToleranceDays);

        if (navSeries[0].date > insufficientCutoff)
            return [];

        var slice = new List<(DateOnly date, decimal nav)>();
        foreach (var point in navSeries)
        {
            if (point.date < windowStart)
                continue;
            if (point.date > asOfDate)
                break;
            slice.Add(point);
        }

        return slice;
    }

    private static async Task WriteHeaderOnlyAsync(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var writer = new StreamWriter(path, append: false, encoding: new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteLineAsync(CsvHeader);
    }

    private static async Task WriteCsvAsync(string path, List<FundSnapshotStatistics> snapshots)
    {
        await using var writer = new StreamWriter(path, append: false, encoding: new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await writer.WriteLineAsync(CsvHeader);

        var seenIsins = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in snapshots)
        {
            if (!seenIsins.Add(s.Isin))
                throw new InvalidOperationException(
                    $"Duplicate ISIN '{s.Isin}' in snapshot output — producer must emit one row per fund.");

            var line = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                s.Isin,
                s.AsOfDate.ToString("yyyy-MM-dd"),
                FormatMetric(s.Return12wCompoundPct),
                FormatMetric(s.AnnVolatility12wPct),
                FormatMetric(s.Sharpe12w),
                FormatMetric(s.MaxDrawdown12wPct),
                FormatMetric(s.Return1yCompoundPct),
                FormatMetric(s.AnnVolatility1yPct),
                FormatMetric(s.Sharpe1y),
                FormatMetric(s.MaxDrawdown1yPct));

            await writer.WriteLineAsync(line);
        }
    }

    private static string FormatMetric(double value)
    {
        return double.IsNaN(value)
            ? "NaN"
            : value.ToString("F4", CultureInfo.InvariantCulture);
    }
}
