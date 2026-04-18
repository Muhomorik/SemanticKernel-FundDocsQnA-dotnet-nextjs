namespace YieldRaccoon.Wpf.Configuration;

/// <summary>
/// User-configurable settings that persist between application launches.
/// Stored in %LocalAppData%/YieldRaccoon/settings.json.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Custom database file path. When null, the default from appsettings.json is used.
    /// </summary>
    public string? DatabasePath { get; set; }

    /// <summary>
    /// Selected database provider. When null, the default from appsettings.json is used.
    /// </summary>
    public DatabaseProvider? DatabaseProvider { get; set; }

    /// <summary>
    /// Backend API base URL for DualWrite provider. When null, the default from appsettings.json is used.
    /// </summary>
    public string? BackendApiUrl { get; set; }

    /// <summary>
    /// Backend API key for DualWrite provider. When null, the default from appsettings.json is used.
    /// </summary>
    public string? BackendApiKey { get; set; }

    /// <summary>
    /// Enabled crawler step names for AboutFund collection (e.g. "Select1Month", "Select3Years").
    /// When null, all default steps are enabled. Takes effect immediately without restart.
    /// </summary>
    public List<string>? EnabledCrawlerSteps { get; set; }

    /// <summary>
    /// When true, a Windows scheduled task launches YieldRaccoon daily at <see cref="AutoStartTimeOfDay"/>.
    /// Created in the user subfolder <c>\YieldRaccoon\</c> in Task Scheduler under the current user account.
    /// </summary>
    public bool AutoStartEnabled { get; set; }

    /// <summary>
    /// Local time of day for the daily auto-start trigger. Only the hour and minute components are used.
    /// Null when auto-start is disabled.
    /// </summary>
    public TimeSpan? AutoStartTimeOfDay { get; set; }

    /// <summary>
    /// When true, the scheduled task launches the exe with <c>--auto-list</c> to start the fund list
    /// crawl automatically. When false, the exe is launched without CLI args (interactive launch at
    /// the scheduled time, user has to start the crawl manually).
    /// </summary>
    public bool AutoStartPassAutoListFlag { get; set; }

    /// <summary>
    /// When true, a Windows scheduled task launches YieldRaccoon weekly on
    /// <see cref="WeeklyExportDay"/> at <see cref="WeeklyExportTimeOfDay"/> to run the statistics export.
    /// </summary>
    public bool WeeklyExportEnabled { get; set; }

    /// <summary>
    /// Day of the week the weekly statistics export runs. Defaults to Thursday when not set.
    /// </summary>
    public DayOfWeek? WeeklyExportDay { get; set; }

    /// <summary>
    /// Local time of day for the weekly export trigger. Only the hour and minute components are used.
    /// Null when weekly export is disabled.
    /// </summary>
    public TimeSpan? WeeklyExportTimeOfDay { get; set; }

    /// <summary>
    /// Timestamp (local time) of the last successful weekly export run. Null until the first run completes.
    /// </summary>
    public DateTime? WeeklyExportLastRunAt { get; set; }

    /// <summary>
    /// Row count written by the most recent weekly export (stats rows only, metadata not counted).
    /// Null until the first run completes.
    /// </summary>
    public int? WeeklyExportLastRunRowCount { get; set; }

    /// <summary>
    /// Window size (days) last used in the Statistics Export window. Used as the default on next open
    /// and by the scheduled weekly run. Null falls back to 14 (2 weeks).
    /// </summary>
    public int? StatsExportWindowDays { get; set; }

    /// <summary>
    /// Lookback period (days) last used in the Statistics Export window. Null falls back to 365 (1 year).
    /// </summary>
    public int? StatsExportLookbackDays { get; set; }

    /// <summary>
    /// Minimum number of owners last used in the Statistics Export window. Null falls back to 100.
    /// </summary>
    public int? StatsExportMinOwners { get; set; }

    /// <summary>
    /// Company filter last used in the Statistics Export window. Empty / null = all companies.
    /// </summary>
    public string? StatsExportCompanyFilter { get; set; }

    /// <summary>
    /// Output path for the statistics CSV last used in the Statistics Export window.
    /// Null keeps the auto-generated path based on the current DB location.
    /// </summary>
    public string? StatsExportOutputPath { get; set; }

    /// <summary>
    /// Output path for the metadata CSV last used in the Statistics Export window.
    /// Null keeps the auto-generated path based on the current DB location.
    /// </summary>
    public string? StatsExportMetadataOutputPath { get; set; }
}
