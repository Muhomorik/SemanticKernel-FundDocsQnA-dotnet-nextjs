using System.Globalization;
using Microsoft.Data.Sqlite;
using NLog;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Reads fund profile metadata from a SQLite database (read-only) and writes it to a CSV file.
/// Only funds with <c>Buyable = 1</c> are included.
/// </summary>
public class FundMetadataCsvExportService : IFundMetadataCsvExportService
{
    private const string CsvHeader =
        "isin,name,company_name,currency_code,category,fund_type,is_index_fund,managed_type," +
        "total_fee,management_fee,risk,rating,sharpe_ratio,standard_deviation," +
        "recommended_holding_period,capital,number_of_owners";

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundMetadataCsvExportService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FundMetadataCsvExportService(ILogger logger)
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

        _logger.Info("Starting metadata CSV export: source={0}, dest={1}, company={2}, minOwners={3}",
            sourceDatabasePath, csvOutputPath, companyName ?? "(all)", minNumberOfOwners);

        var connectionString = $"Data Source={sourceDatabasePath};Mode=ReadOnly";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var rows = await ReadAndWriteCsvAsync(connection, csvOutputPath, companyName, minNumberOfOwners);

        await connection.CloseAsync();

        _logger.Info("Metadata CSV export completed: {0} ({1} rows)", csvOutputPath, rows);
        return rows;
    }

    private static async Task<int> ReadAndWriteCsvAsync(
        SqliteConnection connection,
        string csvOutputPath,
        string? companyName,
        int minNumberOfOwners)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT fp.Isin, fp.Name, fp.CompanyName, fp.CurrencyCode, fp.Category,
                   fp.FundType, fp.IsIndexFund, fp.ManagedType, fp.TotalFee,
                   fp.ManagementFee, fp.Risk, fp.Rating, fp.SharpeRatio,
                   fp.StandardDeviation, fp.RecommendedHoldingPeriod, fp.Capital,
                   fp.NumberOfOwners
            FROM FundProfiles fp
            WHERE fp.Buyable = 1
              AND (@company IS NULL OR LOWER(fp.CompanyName) = LOWER(@company))
              AND (fp.NumberOfOwners IS NOT NULL AND fp.NumberOfOwners >= @minOwners)
            ORDER BY fp.Isin
            """;

        command.Parameters.AddWithValue("@company",
            string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName.Trim());
        command.Parameters.AddWithValue("@minOwners", minNumberOfOwners);

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(csvOutputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        await using var writer = new StreamWriter(csvOutputPath, append: false,
            encoding: new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await writer.WriteLineAsync(CsvHeader);

        var rowCount = 0;
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var isin = reader.GetString(0);                                      // Isin (NOT NULL)
            var name = EscapeCsvField(reader.IsDBNull(1) ? "" : reader.GetString(1));  // Name
            var company = EscapeCsvField(GetNullableString(reader, 2));           // CompanyName
            var currency = GetNullableString(reader, 3);                          // CurrencyCode
            var category = EscapeCsvField(GetNullableString(reader, 4));          // Category
            var fundType = EscapeCsvField(GetNullableString(reader, 5));          // FundType
            var isIndexFund = FormatNullableBool(reader, 6);                      // IsIndexFund
            var managedType = EscapeCsvField(GetNullableString(reader, 7));       // ManagedType
            var totalFee = FormatNullableDecimal(reader, 8);                      // TotalFee
            var managementFee = FormatNullableDecimal(reader, 9);                 // ManagementFee
            var risk = FormatNullableInt(reader, 10);                             // Risk
            var rating = FormatNullableInt(reader, 11);                           // Rating
            var sharpeRatio = FormatNullableDecimal(reader, 12);                  // SharpeRatio
            var stdDev = FormatNullableDecimal(reader, 13);                       // StandardDeviation
            var holdingPeriod = EscapeCsvField(GetNullableString(reader, 14));    // RecommendedHoldingPeriod
            var capital = FormatNullableDecimal(reader, 15);                      // Capital
            var owners = FormatNullableInt(reader, 16);                           // NumberOfOwners

            var line = $"{isin},{name},{company},{currency},{category},{fundType},{isIndexFund}," +
                       $"{managedType},{totalFee},{managementFee},{risk},{rating},{sharpeRatio}," +
                       $"{stdDev},{holdingPeriod},{capital},{owners}";

            await writer.WriteLineAsync(line);
            rowCount++;
        }

        return rowCount;
    }

    private static string GetNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);

    private static string FormatNullableInt(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? "" : reader.GetInt64(ordinal).ToString(CultureInfo.InvariantCulture);

    private static string FormatNullableDecimal(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? "" : reader.GetDouble(ordinal).ToString(CultureInfo.InvariantCulture);

    private static string FormatNullableBool(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return "";

        return reader.GetInt64(ordinal) != 0 ? "true" : "false";
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
