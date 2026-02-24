namespace YieldRaccoon.Application.Services;

/// <summary>
/// Service for exporting filtered fund data to a standalone SQLite database file.
/// </summary>
/// <remarks>
/// <para>Export pipeline:</para>
/// <list type="number">
///   <item>Copy the source database file to the destination path (original is never modified)</item>
///   <item>Delete FundProfiles not matching the specified company name</item>
///   <item>Delete orphaned FundHistoryRecords (no matching FundProfile)</item>
///   <item>Delete FundHistoryRecords older than the cutoff date</item>
///   <item>VACUUM to reclaim disk space</item>
/// </list>
/// </remarks>
public interface IFundDataExportService
{
    /// <summary>
    /// Exports fund data for a specific company and time period to a new database file.
    /// </summary>
    /// <param name="sourcePath">Path to the source SQLite database file.</param>
    /// <param name="destinationPath">Path where the filtered database will be saved.</param>
    /// <param name="companyName">Company name to keep (case-insensitive match).</param>
    /// <param name="cutoffDate">Oldest date to keep — records with NavDate before this are removed.</param>
    Task ExportAsync(string sourcePath, string destinationPath, string companyName, DateOnly cutoffDate);
}
