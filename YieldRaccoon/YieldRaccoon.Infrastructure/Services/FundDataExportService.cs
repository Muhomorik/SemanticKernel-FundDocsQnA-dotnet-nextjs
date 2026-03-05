using Microsoft.Data.Sqlite;
using NLog;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Exports filtered fund data to a standalone SQLite database file.
/// Copies the source database and removes non-matching records from the copy.
/// </summary>
public class FundDataExportService : IFundDataExportService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundDataExportService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FundDataExportService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ExportAsync(string sourcePath, string destinationPath, string? companyName, DateOnly cutoffDate, int minNumberOfOwners = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source database file not found.", sourcePath);

        _logger.Info("Starting export: company={0}, cutoff={1}, minOwners={2}, source={3}, dest={4}",
            companyName, cutoffDate, minNumberOfOwners, sourcePath, destinationPath);

        // Ensure destination directory exists
        var destinationDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDir))
            Directory.CreateDirectory(destinationDir);

        // Step 1: Copy source database to destination (never touches original)
        File.Copy(sourcePath, destinationPath, overwrite: true);
        _logger.Debug("Database copied to {0}", destinationPath);

        // Also copy WAL/SHM journal files if they exist (ensures consistent copy)
        CopyJournalFileIfExists(sourcePath + "-wal", destinationPath + "-wal");
        CopyJournalFileIfExists(sourcePath + "-shm", destinationPath + "-shm");

        // Step 2: Open the copy and filter data
        var connectionString = $"Data Source={destinationPath}";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Checkpoint WAL to merge journal into main database file
        await ExecuteNonQueryAsync(connection, "PRAGMA wal_checkpoint(TRUNCATE)");
        _logger.Debug("WAL checkpoint completed");

        // Delete funds not matching the company name (case-insensitive), skip if no company specified
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            var deletedProfiles = await ExecuteNonQueryAsync(connection,
                "DELETE FROM FundProfiles WHERE CompanyName IS NULL OR LOWER(CompanyName) != LOWER(@company)",
                ("@company", companyName));
            _logger.Info("Deleted {0} non-matching FundProfiles", deletedProfiles);
        }
        else
        {
            _logger.Info("No company filter specified — keeping all companies");
        }

        // Delete funds with fewer owners than the minimum threshold
        if (minNumberOfOwners > 0)
        {
            var deletedByOwners = await ExecuteNonQueryAsync(connection,
                "DELETE FROM FundProfiles WHERE NumberOfOwners IS NULL OR NumberOfOwners < @minOwners",
                ("@minOwners", minNumberOfOwners));
            _logger.Info("Deleted {0} FundProfiles below min owners threshold ({1})", deletedByOwners, minNumberOfOwners);
        }

        // Delete orphaned history records (fund no longer exists in the filtered set)
        var deletedOrphans = await ExecuteNonQueryAsync(connection,
            "DELETE FROM FundHistoryRecords WHERE FundId NOT IN (SELECT Isin FROM FundProfiles)");
        _logger.Info("Deleted {0} orphaned FundHistoryRecords", deletedOrphans);

        // Delete history records older than the cutoff date
        var cutoffString = cutoffDate.ToString("yyyy-MM-dd");
        var deletedOld = await ExecuteNonQueryAsync(connection,
            "DELETE FROM FundHistoryRecords WHERE NavDate < @cutoff",
            ("@cutoff", cutoffString));
        _logger.Info("Deleted {0} FundHistoryRecords before {1}", deletedOld, cutoffString);

        // Switch from WAL to DELETE journal mode — this checkpoints pending changes into the main file
        await ExecuteNonQueryAsync(connection, "PRAGMA journal_mode=DELETE");
        _logger.Debug("Switched to DELETE journal mode");

        // Reclaim disk space (now operates directly on the main file, not WAL)
        await ExecuteNonQueryAsync(connection, "VACUUM");
        _logger.Debug("VACUUM completed");

        await connection.CloseAsync();
        SqliteConnection.ClearPool(connection);

        // Clean up leftover journal files
        CleanupJournalFile(destinationPath + "-wal");
        CleanupJournalFile(destinationPath + "-shm");

        var exportSize = new FileInfo(destinationPath).Length;
        _logger.Info("Export completed successfully: {0} ({1:N0} bytes)", destinationPath, exportSize);
    }

    private static async Task<int> ExecuteNonQueryAsync(SqliteConnection connection, string sql,
        params (string name, object value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);

        return await command.ExecuteNonQueryAsync();
    }

    private void CopyJournalFileIfExists(string sourcePath, string destinationPath)
    {
        if (File.Exists(sourcePath))
        {
            File.Copy(sourcePath, destinationPath, overwrite: true);
            _logger.Debug("Copied journal file: {0}", Path.GetFileName(sourcePath));
        }
    }

    private static void CleanupJournalFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup — not critical
        }
    }
}
