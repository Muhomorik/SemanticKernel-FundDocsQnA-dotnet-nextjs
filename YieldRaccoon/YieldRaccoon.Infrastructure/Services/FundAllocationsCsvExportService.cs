using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using NLog;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Reads country + sector portfolio allocations from a SQLite database (read-only) and writes a
/// wide-format CSV file with one row per fund and one column per country/sector.
/// </summary>
/// <remarks>
/// The column set is discovered at export time from the <c>Countries</c> and <c>Sectors</c> lookup
/// tables. Country columns precede sector columns; within each block, columns are alphabetically
/// sorted by sanitized name. Funds with no rows in either allocation table are excluded — the
/// portfolio page hasn't been crawled for them.
/// </remarks>
public class FundAllocationsCsvExportService : IFundAllocationsCsvExportService
{
    private const string CountryColumnPrefix = "country_";
    private const string SectorColumnPrefix = "sector_";

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundAllocationsCsvExportService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FundAllocationsCsvExportService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> ExportAsync(
        string sourceDatabasePath,
        string csvOutputPath,
        string? companyName = null,
        int minNumberOfOwners = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvOutputPath);

        if (!File.Exists(sourceDatabasePath))
            throw new FileNotFoundException("Source database file not found.", sourceDatabasePath);

        _logger.Info("Starting allocations CSV export: source={0}, dest={1}, company={2}, minOwners={3}",
            sourceDatabasePath, csvOutputPath, companyName ?? "(all)", minNumberOfOwners);

        var connectionString = $"Data Source={sourceDatabasePath};Mode=ReadOnly";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var countryColumns = await DiscoverColumnsAsync(connection,
            "SELECT DisplayName FROM Countries ORDER BY DisplayName", CountryColumnPrefix, "country");
        var sectorColumns = await DiscoverColumnsAsync(connection,
            "SELECT DisplayName FROM Sectors ORDER BY DisplayName", SectorColumnPrefix, "sector");

        var funds = await FundQueryHelpers.ReadFundProfilesAsync(connection, companyName, minNumberOfOwners);

        var countryAllocations = await ReadAllocationsAsync(
            connection,
            """
            SELECT fp.Isin, c.DisplayName, fca.Percentage
            FROM FundCountryAllocations fca
            INNER JOIN FundProfiles fp ON fca.Isin = fp.Isin
            INNER JOIN Countries c     ON fca.CountryId = c.CountryId
            WHERE fp.Buyable = 1
              AND (@company IS NULL OR LOWER(fp.CompanyName) = LOWER(@company))
              AND (fp.NumberOfOwners IS NOT NULL AND fp.NumberOfOwners >= @minOwners)
            """,
            countryColumns.ColumnByDisplayName,
            companyName,
            minNumberOfOwners);

        var sectorAllocations = await ReadAllocationsAsync(
            connection,
            """
            SELECT fp.Isin, s.DisplayName, fsa.Percentage
            FROM FundSectorAllocations fsa
            INNER JOIN FundProfiles fp ON fsa.Isin = fp.Isin
            INNER JOIN Sectors s       ON fsa.SectorId = s.SectorId
            WHERE fp.Buyable = 1
              AND (@company IS NULL OR LOWER(fp.CompanyName) = LOWER(@company))
              AND (fp.NumberOfOwners IS NOT NULL AND fp.NumberOfOwners >= @minOwners)
            """,
            sectorColumns.ColumnByDisplayName,
            companyName,
            minNumberOfOwners);

        var rows = await WriteCsvAsync(
            csvOutputPath,
            funds,
            countryColumns.SortedColumns,
            sectorColumns.SortedColumns,
            countryAllocations,
            sectorAllocations);

        await connection.CloseAsync();

        _logger.Info("Allocations CSV export completed: {0} ({1} rows, {2} country cols, {3} sector cols)",
            csvOutputPath, rows, countryColumns.SortedColumns.Count, sectorColumns.SortedColumns.Count);

        return rows;
    }

    private static async Task<DiscoveredColumns> DiscoverColumnsAsync(
        SqliteConnection connection,
        string sql,
        string columnPrefix,
        string kindLabel)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var columnByDisplayName = new Dictionary<string, string>(StringComparer.Ordinal);
        var displayNameByColumn = new Dictionary<string, string>(StringComparer.Ordinal);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (reader.IsDBNull(0))
                continue;

            var displayName = reader.GetString(0);
            var column = columnPrefix + AllocationColumnSanitizer.Sanitize(displayName);

            if (displayNameByColumn.TryGetValue(column, out var existing))
            {
                throw new InvalidOperationException(
                    $"Two {kindLabel} display names sanitize to the same column '{column}': '{existing}' and '{displayName}'. " +
                    $"Resolve the ambiguity in the source data before re-running the export.");
            }

            columnByDisplayName[displayName] = column;
            displayNameByColumn[column] = displayName;
        }

        var sorted = displayNameByColumn.Keys
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        return new DiscoveredColumns(sorted, columnByDisplayName);
    }

    private static async Task<Dictionary<string, Dictionary<string, decimal>>> ReadAllocationsAsync(
        SqliteConnection connection,
        string sql,
        IReadOnlyDictionary<string, string> columnByDisplayName,
        string? companyName,
        int minNumberOfOwners)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@company",
            string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName.Trim());
        command.Parameters.AddWithValue("@minOwners", minNumberOfOwners);

        var byIsin = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.Ordinal);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var isin = reader.GetString(0);
            var displayName = reader.GetString(1);
            var percentage = reader.GetDecimal(2);

            if (!columnByDisplayName.TryGetValue(displayName, out var column))
                continue;

            if (!byIsin.TryGetValue(isin, out var inner))
            {
                inner = new Dictionary<string, decimal>(StringComparer.Ordinal);
                byIsin[isin] = inner;
            }

            inner[column] = percentage;
        }

        return byIsin;
    }

    private static async Task<int> WriteCsvAsync(
        string csvOutputPath,
        IReadOnlyList<(string isin, string name)> funds,
        IReadOnlyList<string> countryColumns,
        IReadOnlyList<string> sectorColumns,
        IReadOnlyDictionary<string, Dictionary<string, decimal>> countryAllocations,
        IReadOnlyDictionary<string, Dictionary<string, decimal>> sectorAllocations)
    {
        var outputDir = Path.GetDirectoryName(csvOutputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        await using var writer = new StreamWriter(csvOutputPath, append: false,
            encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var header = new StringBuilder("isin,name");
        foreach (var col in countryColumns) header.Append(',').Append(col);
        foreach (var col in sectorColumns) header.Append(',').Append(col);
        await writer.WriteLineAsync(header.ToString());

        var rowCount = 0;
        var line = new StringBuilder();

        foreach (var (isin, name) in funds)
        {
            var hasCountry = countryAllocations.TryGetValue(isin, out var countryDict);
            var hasSector = sectorAllocations.TryGetValue(isin, out var sectorDict);

            if (!hasCountry && !hasSector)
                continue;

            line.Clear();
            line.Append(isin).Append(',').Append(EscapeCsvField(name));

            AppendAllocationCells(line, countryColumns, countryDict);
            AppendAllocationCells(line, sectorColumns, sectorDict);

            await writer.WriteLineAsync(line.ToString());
            rowCount++;
        }

        return rowCount;
    }

    private static void AppendAllocationCells(
        StringBuilder line,
        IReadOnlyList<string> columns,
        Dictionary<string, decimal>? values)
    {
        foreach (var col in columns)
        {
            line.Append(',');
            if (values is not null && values.TryGetValue(col, out var v))
                line.Append(v.ToString(CultureInfo.InvariantCulture));
            else
                line.Append('0');
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

    private sealed record DiscoveredColumns(
        IReadOnlyList<string> SortedColumns,
        IReadOnlyDictionary<string, string> ColumnByDisplayName);
}
