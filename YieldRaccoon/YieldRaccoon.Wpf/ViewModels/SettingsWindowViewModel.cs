using System.IO;
using System.Windows.Input;
using DevExpress.Mvvm;
using Microsoft.Win32;
using NLog;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Services;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the settings window.
/// Allows users to configure database location and other preferences.
/// </summary>
public class SettingsWindowViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IUserSettingsService _settingsService;
    private readonly DatabaseOptions _databaseOptions;
    private readonly string _originalDatabasePath;

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

        // Extract the database path from the connection string
        _originalDatabasePath = ExtractDatabasePath(databaseOptions.ConnectionString);
        DatabasePath = userSettings?.DatabasePath ?? _originalDatabasePath;

        // Initialize commands
        BrowseCommand = new DelegateCommand(ExecuteBrowse);
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
        _originalDatabasePath = "YieldRaccoon.db";
        DatabasePath = _originalDatabasePath;

        BrowseCommand = new DelegateCommand(() => { });
        SaveCommand = new DelegateCommand(() => { });
        CancelCommand = new DelegateCommand(() => { });
    }

    #region Properties

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
    public bool HasChanges => !string.Equals(DatabasePath, _originalDatabasePath, StringComparison.OrdinalIgnoreCase);

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

    private bool CanExecuteSave()
    {
        return !string.IsNullOrWhiteSpace(DatabasePath);
    }

    private void ExecuteSave()
    {
        try
        {
            _logger.Info($"Saving settings - Database path: {DatabasePath}");

            var settings = new UserSettings
            {
                DatabasePath = DatabasePath
            };

            _settingsService.Save(settings);

            _logger.Info("Settings saved successfully");
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings");
            // Could show error dialog here
        }
    }

    private void ExecuteCancel()
    {
        _logger.Debug("Settings cancelled");
        CloseRequested?.Invoke(this, false);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Extracts the database file path from a SQLite connection string.
    /// </summary>
    private static string ExtractDatabasePath(string connectionString)
    {
        // Connection string format: "Data Source=path/to/database.db"
        const string dataSourcePrefix = "Data Source=";

        if (string.IsNullOrWhiteSpace(connectionString))
            return "YieldRaccoon.db";

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
