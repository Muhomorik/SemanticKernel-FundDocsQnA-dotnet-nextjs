using System.IO;
using System.Windows.Input;
using DevExpress.Mvvm;
using Microsoft.Win32;
using NLog;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Models;
using YieldRaccoon.Wpf.Services;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the settings window.
/// Allows users to configure database provider, location, and other preferences.
/// </summary>
public class SettingsWindowViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IUserSettingsService _settingsService;
    private readonly DatabaseOptions _databaseOptions;
    private readonly string _originalDatabasePath;
    private readonly DatabaseProvider _originalProvider;
    private readonly string _originalBackendApiUrl;
    private readonly string _originalBackendApiKey;

    /// <summary>
    /// Event raised when the window should close with a result.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindowViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="settingsService">Service for loading and saving user settings.</param>
    /// <param name="databaseOptions">Current database configuration.</param>
    /// <param name="userSettings">Current user settings.</param>
    public SettingsWindowViewModel(
        ILogger logger,
        IUserSettingsService settingsService,
        DatabaseOptions databaseOptions,
        UserSettings userSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));

        // Initialize provider options
        AvailableProviders = CreateProviderOptions();
        _originalProvider = databaseOptions.Provider;
        SelectedProvider = AvailableProviders.First(p => p.Provider == databaseOptions.Provider);

        // Extract the database path from the connection string
        _originalDatabasePath = ExtractDatabasePath(databaseOptions.ConnectionString);
        DatabasePath = userSettings?.DatabasePath ?? _originalDatabasePath;

        // Initialize Backend API settings for DualWrite
        _originalBackendApiUrl = databaseOptions.BackendApiUrl ?? string.Empty;
        _originalBackendApiKey = databaseOptions.BackendApiKey ?? string.Empty;
        BackendApiUrl = userSettings?.BackendApiUrl ?? _originalBackendApiUrl;
        BackendApiKey = userSettings?.BackendApiKey ?? _originalBackendApiKey;

        // Initialize commands
        BrowseCommand = new DelegateCommand(ExecuteBrowse);
        ResetToDefaultCommand = new DelegateCommand(ExecuteResetToDefault);
        SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave);
        CancelCommand = new DelegateCommand(ExecuteCancel);

        _logger.Debug("SettingsWindowViewModel initialized");
    }

    /// <summary>
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public SettingsWindowViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _settingsService = null!;
        _databaseOptions = new DatabaseOptions();
        _originalDatabasePath = DatabaseOptions.DefaultDatabaseFileName;
        _originalProvider = DatabaseProvider.DualWrite;
        _originalBackendApiUrl = string.Empty;
        _originalBackendApiKey = string.Empty;
        DatabasePath = _originalDatabasePath;

        AvailableProviders = CreateProviderOptions();
        SelectedProvider = AvailableProviders[0];

        BrowseCommand = new DelegateCommand(() => { });
        ResetToDefaultCommand = new DelegateCommand(() => { });
        SaveCommand = new DelegateCommand(() => { });
        CancelCommand = new DelegateCommand(() => { });
    }

    #region Properties

    /// <summary>
    /// Gets the available database providers for the ComboBox.
    /// </summary>
    public IReadOnlyList<DatabaseProviderOption> AvailableProviders { get; }

    /// <summary>
    /// Gets or sets the selected database provider.
    /// </summary>
    public DatabaseProviderOption SelectedProvider
    {
        get => GetProperty(() => SelectedProvider);
        set
        {
            if (SetProperty(() => SelectedProvider, value))
            {
                RaisePropertyChanged(() => HasChanges);
                RaisePropertyChanged(() => RestartRequiredMessage);
                RaisePropertyChanged(() => IsDatabasePathVisible);
                RaisePropertyChanged(() => IsBackendApiVisible);
            }
        }
    }

    /// <summary>
    /// Gets whether the database path field should be visible.
    /// Only relevant for SQLite and DualWrite providers.
    /// </summary>
    public bool IsDatabasePathVisible =>
        SelectedProvider?.Provider is DatabaseProvider.SQLite or DatabaseProvider.DualWrite;

    /// <summary>
    /// Gets whether the Backend API settings should be visible.
    /// Only relevant for the DualWrite provider.
    /// </summary>
    public bool IsBackendApiVisible =>
        SelectedProvider?.Provider is DatabaseProvider.DualWrite;

    /// <summary>
    /// Gets or sets the Backend API base URL.
    /// </summary>
    public string BackendApiUrl
    {
        get => GetProperty(() => BackendApiUrl);
        set
        {
            if (SetProperty(() => BackendApiUrl, value))
            {
                RaisePropertyChanged(() => HasChanges);
                RaisePropertyChanged(() => RestartRequiredMessage);
            }
        }
    }

    /// <summary>
    /// Gets or sets the Backend API key.
    /// </summary>
    public string BackendApiKey
    {
        get => GetProperty(() => BackendApiKey);
        set
        {
            if (SetProperty(() => BackendApiKey, value))
            {
                RaisePropertyChanged(() => HasChanges);
                RaisePropertyChanged(() => RestartRequiredMessage);
            }
        }
    }

    /// <summary>
    /// Gets or sets the database file path.
    /// </summary>
    public string DatabasePath
    {
        get => GetProperty(() => DatabasePath);
        set
        {
            if (SetProperty(() => DatabasePath, value))
            {
                RaisePropertyChanged(() => HasChanges);
                RaisePropertyChanged(() => RestartRequiredMessage);
            }
        }
    }

    /// <summary>
    /// Gets whether there are unsaved changes that require a restart.
    /// </summary>
    public bool HasChanges =>
        !string.Equals(DatabasePath, _originalDatabasePath, StringComparison.OrdinalIgnoreCase)
        || SelectedProvider?.Provider != _originalProvider
        || !string.Equals(BackendApiUrl ?? string.Empty, _originalBackendApiUrl, StringComparison.Ordinal)
        || !string.Equals(BackendApiKey ?? string.Empty, _originalBackendApiKey, StringComparison.Ordinal);

    /// <summary>
    /// Gets the restart required message, shown when settings have changed.
    /// </summary>
    public string RestartRequiredMessage => HasChanges
        ? "Restart required for changes to take effect"
        : string.Empty;

    /// <summary>
    /// Gets the path to the user settings file.
    /// </summary>
    public string SettingsFilePath => _settingsService?.SettingsFilePath ?? string.Empty;

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to browse for a database file.
    /// </summary>
    public ICommand BrowseCommand { get; }

    /// <summary>
    /// Gets the command to reset the database path to the factory default.
    /// </summary>
    public ICommand ResetToDefaultCommand { get; }

    /// <summary>
    /// Gets the command to save settings and close.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Gets the command to cancel and close without saving.
    /// </summary>
    public ICommand CancelCommand { get; }

    #endregion

    #region Command Implementations

    private void ExecuteBrowse()
    {
        _logger.Debug("Browse for database file");

        var dialog = new SaveFileDialog
        {
            Title = "Select Database Location",
            Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
            DefaultExt = ".db",
            FileName = Path.GetFileName(DatabasePath),
            InitialDirectory = GetInitialDirectory(),
            OverwritePrompt = false
        };

        if (dialog.ShowDialog() == true)
        {
            DatabasePath = dialog.FileName;
            _logger.Info($"Selected database path: {DatabasePath}");
        }
    }

    private string GetInitialDirectory()
    {
        try
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                return directory;
            }
        }
        catch
        {
            // Ignore path errors
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private void ExecuteResetToDefault()
    {
        SelectedProvider = AvailableProviders.First(p => p.Provider == DatabaseProvider.InMemory);
        DatabasePath = DatabaseOptions.DefaultDatabaseFileName;
        BackendApiUrl = string.Empty;
        BackendApiKey = string.Empty;
        _logger.Debug("Settings reset to defaults");
    }

    private bool CanExecuteSave()
    {
        return !string.IsNullOrWhiteSpace(DatabasePath);
    }

    private void ExecuteSave()
    {
        try
        {
            _logger.Info($"Saving settings - Provider: {SelectedProvider.Provider}, Database path: {DatabasePath}");

            var settings = new UserSettings
            {
                DatabaseProvider = SelectedProvider.Provider,
                DatabasePath = DatabasePath,
                BackendApiUrl = string.IsNullOrWhiteSpace(BackendApiUrl) ? null : BackendApiUrl.TrimEnd('/'),
                BackendApiKey = string.IsNullOrWhiteSpace(BackendApiKey) ? null : BackendApiKey
            };

            _settingsService.Save(settings);

            _logger.Info("Settings saved successfully");
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings");
        }
    }

    private void ExecuteCancel()
    {
        _logger.Debug("Settings cancelled");
        CloseRequested?.Invoke(this, false);
    }

    #endregion

    #region Helpers

    private static IReadOnlyList<DatabaseProviderOption> CreateProviderOptions() =>
    [
        new DatabaseProviderOption(DatabaseProvider.InMemory, "InMemory"),
        new DatabaseProviderOption(DatabaseProvider.SQLite, "SQLite"),
        new DatabaseProviderOption(DatabaseProvider.DualWrite, "DualWrite (SQLite + Azure SQL)")
    ];

    /// <summary>
    /// Extracts the database file path from an SQLite connection string.
    /// </summary>
    private static string ExtractDatabasePath(string connectionString)
    {
        // Connection string format: "Data Source=path/to/database.db"
        const string dataSourcePrefix = "Data Source=";

        if (string.IsNullOrWhiteSpace(connectionString))
            return DatabaseOptions.DefaultDatabaseFileName;

        var index = connectionString.IndexOf(dataSourcePrefix, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            var path = connectionString[(index + dataSourcePrefix.Length)..].Trim();
            // Remove any trailing parameters (e.g., ";Mode=...")
            var semicolonIndex = path.IndexOf(';');
            if (semicolonIndex >= 0)
            {
                path = path[..semicolonIndex];
            }
            return path;
        }

        return connectionString;
    }

    #endregion
}
