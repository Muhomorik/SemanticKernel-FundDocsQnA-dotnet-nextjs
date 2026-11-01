using System.Threading.Tasks;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Service for exporting fund country + sector portfolio allocations from a SQLite database to a
/// single wide-format CSV file (one row per fund, one column per country/sector).
/// </summary>
/// <remarks>
/// <para>Reads the source database in read-only mode — nothing is modified.</para>
/// <para>
/// Columns are discovered at export time from the <c>Countries</c> and <c>Sectors</c> lookup
/// tables, so newly-encountered categories show up as new columns automatically. Country columns
/// are prefixed <c>country_</c>; sector columns <c>sector_</c>; both blocks are alphabetically
/// sorted within themselves and country columns precede sector columns.
/// </para>
/// <para>
/// Cells contain decimal percentages (0–100). Missing allocations are emitted as the literal
/// <c>0</c> — the source page lists only non-zero entries, so absence unambiguously means
/// the fund holds none of that category.
/// </para>
/// <para>
/// Filters: only funds with <c>Buyable = 1</c> matching the optional company name and the
/// minimum-owners threshold are considered. A fund is further excluded if it has no rows in
/// either allocation table (the portfolio page hasn't been crawled yet).
/// </para>
/// </remarks>
public interface IFundAllocationsCsvExportService
{
    /// <summary>
    /// Reads country + sector allocation data and writes a wide-format CSV file.
    /// </summary>
    /// <param name="sourceDatabasePath">Path to the SQLite database file.</param>
    /// <param name="csvOutputPath">Path where the CSV file will be written (overwrites if exists).</param>
    /// <param name="companyName">Optional company name filter (case-insensitive). Null or empty for all.</param>
    /// <param name="minNumberOfOwners">Minimum number of owners (0 to skip filter).</param>
    /// <returns>The number of fund rows written to the CSV file (excluding the header).</returns>
    Task<int> ExportAsync(
        string sourceDatabasePath,
        string csvOutputPath,
        string? companyName = null,
        int minNumberOfOwners = 0);
}
