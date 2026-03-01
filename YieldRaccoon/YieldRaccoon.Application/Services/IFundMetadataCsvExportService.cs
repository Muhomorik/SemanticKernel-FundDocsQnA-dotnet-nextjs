using System.Threading.Tasks;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Service for exporting fund profile metadata from a SQLite database to a CSV file.
/// </summary>
/// <remarks>
/// <para>Reads the source database in read-only mode — nothing is modified.</para>
/// <para>
/// Exports one row per qualifying fund with profile attributes:
/// ISIN, name, company, currency, category, fund type, fees, risk metrics, etc.
/// Only funds with <c>Buyable = 1</c> are included.
/// </para>
/// </remarks>
public interface IFundMetadataCsvExportService
{
    /// <summary>
    /// Reads fund profile data and writes a metadata CSV file.
    /// </summary>
    /// <param name="sourceDatabasePath">Path to the SQLite database file.</param>
    /// <param name="csvOutputPath">Path where the CSV file will be written (overwrites if exists).</param>
    /// <param name="companyName">Optional company name filter (case-insensitive). Null or empty for all.</param>
    /// <param name="minNumberOfOwners">Minimum number of owners (0 to skip filter).</param>
    /// <returns>The number of rows written to the CSV file.</returns>
    Task<int> ExportAsync(
        string sourceDatabasePath,
        string csvOutputPath,
        string? companyName = null,
        int minNumberOfOwners = 0);
}
