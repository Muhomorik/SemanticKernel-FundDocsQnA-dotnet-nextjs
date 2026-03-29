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
}
