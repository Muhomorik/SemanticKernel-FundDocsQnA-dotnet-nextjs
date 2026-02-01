using System.IO;
using System.Text.Json;
using NLog;
using YieldRaccoon.Wpf.Configuration;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for persisting user settings to a JSON file in the local application data folder.
/// </summary>
public class UserSettingsService : IUserSettingsService
{
    private readonly ILogger _logger;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string AppFolderName = "YieldRaccoon";
    private const string SettingsFileName = "settings.json";

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public UserSettingsService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsFilePath = Path.Combine(localAppData, AppFolderName, SettingsFileName);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.Debug($"User settings file path: {_settingsFilePath}");
    }

    /// <inheritdoc />
    public string SettingsFilePath => _settingsFilePath;

    /// <inheritdoc />
    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.Debug("Settings file does not exist, returning defaults");
                return new UserSettings();
            }

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json, _jsonOptions);

            if (settings == null)
            {
                _logger.Warn("Failed to deserialize settings, returning defaults");
                return new UserSettings();
            }

            _logger.Info($"Loaded user settings from {_settingsFilePath}");
            return settings;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading user settings, returning defaults");
            return new UserSettings();
        }
    }

    /// <inheritdoc />
    public void Save(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.Debug($"Created settings directory: {directory}");
            }

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsFilePath, json);

            _logger.Info($"Saved user settings to {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving user settings");
            throw;
        }
    }
}
