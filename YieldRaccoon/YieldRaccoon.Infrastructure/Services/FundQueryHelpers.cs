using System.Globalization;
using Microsoft.Data.Sqlite;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Shared SQLite read-only helpers for the CSV export services. Both the per-bucket statistics
/// service and the rolling-horizon snapshot service apply the exact same fund-eligibility filters
/// (Buyable = 1, optional company name, minimum owners) and read the same NAV-history shape — keeping
/// the SQL in one place prevents the two filter sets from silently diverging.
/// </summary>
internal static class FundQueryHelpers
{
    /// <summary>
    /// Reads ISIN + display-name pairs for funds matching the eligibility filters.
    /// </summary>
    /// <param name="connection">An open read-only SQLite connection.</param>
    /// <param name="companyName">Case-insensitive company filter; null/empty includes all companies.</param>
    /// <param name="minNumberOfOwners">Minimum owner count required (0 to skip).</param>
    public static async Task<List<(string isin, string name)>> ReadFundProfilesAsync(
        SqliteConnection connection,
        string? companyName,
        int minNumberOfOwners)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT fp.Isin, fp.Name
            FROM FundProfiles fp
            WHERE fp.Buyable = 1
              AND (@company IS NULL OR LOWER(fp.CompanyName) = LOWER(@company))
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

    /// <summary>
    /// Reads a fund's daily NAV series in chronological order, optionally truncated to records
    /// on or after <paramref name="cutoffDate"/>.
    /// </summary>
    public static async Task<List<(DateOnly date, decimal nav)>> ReadNavSeriesAsync(
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
    /// Returns the most recent NavDate present in <c>FundHistoryRecords</c>, or <c>null</c> when the
    /// table holds no NAV rows. Used by the snapshot service to anchor <c>as_of_date</c>.
    /// </summary>
    public static async Task<DateOnly?> GetLatestNavDateAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT MAX(NavDate)
            FROM FundHistoryRecords
            WHERE Nav IS NOT NULL AND NavDate IS NOT NULL
            """;

        var result = await command.ExecuteScalarAsync();
        if (result is null || result is DBNull)
            return null;

        var dateStr = result.ToString();
        if (string.IsNullOrEmpty(dateStr))
            return null;

        return DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
