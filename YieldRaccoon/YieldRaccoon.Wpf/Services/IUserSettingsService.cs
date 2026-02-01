using YieldRaccoon.Wpf.Configuration;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for loading and saving user settings to persistent storage.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Loads user settings from the settings file.
    /// Returns default settings if the file does not exist.
    /// </summary>
    UserSettings Load();

    /// <summary>
    /// Saves user settings to the settings file.
    /// Creates the directory if it does not exist.
    /// </summary>
    void Save(UserSettings settings);

    /// <summary>
    /// Gets the full path to the settings file.
    /// </summary>
    string SettingsFilePath { get; }
}
