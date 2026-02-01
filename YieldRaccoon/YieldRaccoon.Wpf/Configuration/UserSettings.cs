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
}
