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
}
