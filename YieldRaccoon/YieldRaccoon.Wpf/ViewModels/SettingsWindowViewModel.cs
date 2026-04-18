using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using Microsoft.Win32;
using NLog;
using YieldRaccoon.Application.Models;
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
    private const int HResultAccessDenied = unchecked((int)0x80070005);

    private readonly ILogger _logger;
    private readonly IUserSettingsService _settingsService;
    private readonly IAutoStartSchedulerService _autoStartScheduler;
    private readonly UserSettings _userSettings;
    private readonly DatabaseOptions _databaseOptions;
    private readonly string _originalDatabasePath;
    private readonly DatabaseProvider _originalProvider;
    private readonly string _originalBackendApiUrl;
    private readonly string _originalBackendApiKey;
    private readonly IReadOnlySet<AboutFundCollectionStepKind> _originalEnabledSteps;

    /// <summary>
    /// Event raised when the window should close with a result.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindowViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="settingsService">Service for loading and saving user settings.</param>
    /// <param name="autoStartScheduler">Service managing the Windows scheduled task for daily auto-start.</param>
    /// <param name="databaseOptions">Current database configuration.</param>
    /// <param name="userSettings">Current user settings.</param>
    public SettingsWindowViewModel(
        ILogger logger,
        IUserSettingsService settingsService,
        IAutoStartSchedulerService autoStartScheduler,
        DatabaseOptions databaseOptions,
        UserSettings userSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _autoStartScheduler = autoStartScheduler ?? throw new ArgumentNullException(nameof(autoStartScheduler));
        _userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));

        // Initialize provider options
        AvailableProviders = CreateProviderOptions();
        _originalProvider = databaseOptions.Provider;
        SelectedProvider = AvailableProviders.First(p => p.Provider == databaseOptions.Provider);

        // Extract the database path from the connection string
        _originalDatabasePath = ExtractDatabasePath(databaseOptions.ConnectionString);
        DatabasePath = userSettings.DatabasePath ?? _originalDatabasePath;

        // Initialize Backend API settings for DualWrite
        _originalBackendApiUrl = databaseOptions.BackendApiUrl ?? string.Empty;
        _originalBackendApiKey = databaseOptions.BackendApiKey ?? string.Empty;
        BackendApiUrl = userSettings.BackendApiUrl ?? _originalBackendApiUrl;
        BackendApiKey = userSettings.BackendApiKey ?? _originalBackendApiKey;

        // Initialize crawler step toggles from persisted settings
        _originalEnabledSteps = AboutFundCollectionStepKinds.FromNames(userSettings.EnabledCrawlerSteps);
        foreach (var step in AboutFundCollectionStepKinds.Configurable)
            StepToggles.Add(new AboutFundStepToggleViewModel(step, _originalEnabledSteps.Contains(step)));

        // Initialize auto-start settings from persisted state, with a default time of 20:00.
        var initialTime = userSettings.AutoStartTimeOfDay ?? new TimeSpan(20, 0, 0);
        AutoStartTime = DateTime.Today.Add(initialTime);
        AutoStartPassAutoListFlag = userSettings.AutoStartPassAutoListFlag;
        AutoStartEnabled = userSettings.AutoStartEnabled;

        // Weekly statistics export — defaults Thursday 22:00.
        DaysOfWeek = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };
        WeeklyExportDay = userSettings.WeeklyExportDay ?? DayOfWeek.Thursday;
        var initialWeeklyTime = userSettings.WeeklyExportTimeOfDay ?? new TimeSpan(22, 0, 0);
        WeeklyExportTime = DateTime.Today.Add(initialWeeklyTime);
        WeeklyExportEnabled = userSettings.WeeklyExportEnabled;
        UpdateWeeklyExportSummary();

        // Reconcile with the actual scheduled-task state — user may have deleted the task externally.
        try
        {
            if (AutoStartEnabled && !_autoStartScheduler.IsEnabled())
            {
                _logger.Warn("Auto-start flag was set but the scheduled task is missing — flipping off");
                AutoStartEnabled = false;
            }
            if (WeeklyExportEnabled && !_autoStartScheduler.IsWeeklyStatsExportEnabled())
            {
                _logger.Warn("Weekly export flag was set but the scheduled task is missing — flipping off");
                WeeklyExportEnabled = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Failed to reconcile auto-start state with Task Scheduler");
        }

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
        _autoStartScheduler = null!;
        _userSettings = new UserSettings();
        _databaseOptions = new DatabaseOptions();
        _originalDatabasePath = DatabaseOptions.DefaultDatabaseFileName;
        _originalProvider = DatabaseProvider.DualWrite;
        _originalBackendApiUrl = string.Empty;
        _originalBackendApiKey = string.Empty;
        _originalEnabledSteps = AboutFundCollectionStepKinds.Defaults;
        DatabasePath = _originalDatabasePath;

        AvailableProviders = CreateProviderOptions();
        SelectedProvider = AvailableProviders[0];

        foreach (var step in AboutFundCollectionStepKinds.Configurable)
            StepToggles.Add(new AboutFundStepToggleViewModel(step, true));

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
                RaisePropertyChanged(() => HasRestartChanges);
                RaisePropertyChanged(() => HasChanges);
                RaisePropertyChanged(() => RestartRequiredMessage);
                RaisePropertyChanged(() => IsDatabasePathVisible);
                RaisePropertyChanged(() => IsBackendApiVisible);
                RaisePropertyChanged(() => IsBackendApiUrlInvalid);
                RaisePropertyChanged(() => BackendApiUrlError);
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
                RaisePropertyChanged(() => HasRestartChanges);
                RaisePropertyChanged(() => HasChanges);
                RaisePropertyChanged(() => RestartRequiredMessage);
                RaisePropertyChanged(() => IsBackendApiUrlInvalid);
                RaisePropertyChanged(() => BackendApiUrlError);
            }
        }
    }

    /// <summary>
    /// Gets whether the Backend API URL is non-empty but invalid.
    /// </summary>
    public bool IsBackendApiUrlInvalid =>
        IsBackendApiVisible && !string.IsNullOrWhiteSpace(BackendApiUrl) && !IsValidAbsoluteUrl(BackendApiUrl);

    /// <summary>
    /// Gets the validation error message for the Backend API URL.
    /// </summary>
    public string BackendApiUrlError =>
        IsBackendApiUrlInvalid ? "Invalid URL — expected format: https://your-app.azurewebsites.net" : string.Empty;

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
                RaisePropertyChanged(() => HasRestartChanges);
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
                RaisePropertyChanged(() => HasRestartChanges);
                RaisePropertyChanged(() => HasChanges);
                RaisePropertyChanged(() => RestartRequiredMessage);
            }
        }
    }

    /// <summary>
    /// Gets the crawler step toggles for configuring default enabled steps.
    /// </summary>
    public ObservableCollection<AboutFundStepToggleViewModel> StepToggles { get; } = new();

    /// <summary>
    /// Gets whether there are unsaved changes that require a restart (DB/API settings).
    /// </summary>
    public bool HasRestartChanges =>
        !string.Equals(DatabasePath, _originalDatabasePath, StringComparison.OrdinalIgnoreCase)
        || SelectedProvider?.Provider != _originalProvider
        || !string.Equals(BackendApiUrl ?? string.Empty, _originalBackendApiUrl, StringComparison.Ordinal)
        || !string.Equals(BackendApiKey ?? string.Empty, _originalBackendApiKey, StringComparison.Ordinal);

    /// <summary>
    /// Gets whether the crawler step selection differs from the persisted state.
    /// </summary>
    public bool HasStepChanges
    {
        get
        {
            var currentEnabled = new HashSet<AboutFundCollectionStepKind>(
                StepToggles.Where(t => t.IsEnabled).Select(t => t.StepKind));
            return !currentEnabled.SetEquals(_originalEnabledSteps);
        }
    }

    /// <summary>
    /// Gets whether there are any unsaved changes (restart-requiring or step-only).
    /// </summary>
    public bool HasChanges => HasRestartChanges || HasStepChanges;

    /// <summary>
    /// Gets the restart required message, shown when DB/API settings have changed.
    /// </summary>
    public string RestartRequiredMessage => HasRestartChanges
        ? "Restart required for changes to take effect"
        : string.Empty;

    /// <summary>
    /// Gets the path to the user settings file.
    /// </summary>
    public string SettingsFilePath => _settingsService?.SettingsFilePath ?? string.Empty;

    /// <summary>
    /// Gets or sets whether daily auto-start is enabled. When true, saving will register a
    /// Windows scheduled task that launches YieldRaccoon daily at <see cref="AutoStartHour"/>:<see cref="AutoStartMinute"/>.
    /// </summary>
    public bool AutoStartEnabled
    {
        get => GetProperty(() => AutoStartEnabled);
        set => SetProperty(() => AutoStartEnabled, value);
    }

    /// <summary>
    /// Gets or sets the daily auto-start time, backed by a <see cref="DateTime"/> because
    /// <c>mah:TimePicker.SelectedDateTime</c> is DateTime-based. Only the hour and minute
    /// components of the value are persisted — the date portion is ignored.
    /// </summary>
    public DateTime? AutoStartTime
    {
        get => GetProperty(() => AutoStartTime);
        set => SetProperty(() => AutoStartTime, value);
    }

    /// <summary>
    /// Gets or sets whether the scheduled task should pass <c>--auto-list</c> to the launched exe
    /// (starting the fund list crawl automatically). When false, the exe is launched interactively.
    /// </summary>
    public bool AutoStartPassAutoListFlag
    {
        get => GetProperty(() => AutoStartPassAutoListFlag);
        set => SetProperty(() => AutoStartPassAutoListFlag, value);
    }

    /// <summary>
    /// Gets or sets the inline error banner text shown when the scheduled-task operation fails.
    /// Null or empty when there is no error.
    /// </summary>
    public string? AutoStartError
    {
        get => GetProperty(() => AutoStartError);
        set
        {
            if (SetProperty(() => AutoStartError, value))
                RaisePropertyChanged(() => IsAutoStartErrorVisible);
        }
    }

    /// <summary>
    /// Gets whether the auto-start error banner should be visible.
    /// </summary>
    public bool IsAutoStartErrorVisible => !string.IsNullOrEmpty(AutoStartError);

    /// <summary>
    /// Gets the days of the week to populate the weekly export day ComboBox.
    /// </summary>
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; } = Array.Empty<DayOfWeek>();

    /// <summary>
    /// Gets or sets whether the weekly statistics export is enabled. When true, saving creates a
    /// Windows scheduled task that launches YieldRaccoon weekly on <see cref="WeeklyExportDay"/>
    /// at <see cref="WeeklyExportTime"/> with the <c>--auto-weekly-stats</c> flag.
    /// </summary>
    public bool WeeklyExportEnabled
    {
        get => GetProperty(() => WeeklyExportEnabled);
        set => SetProperty(() => WeeklyExportEnabled, value);
    }

    /// <summary>
    /// Gets or sets the weekly export day. Defaults to Thursday.
    /// </summary>
    public DayOfWeek WeeklyExportDay
    {
        get => GetProperty(() => WeeklyExportDay);
        set => SetProperty(() => WeeklyExportDay, value);
    }

    /// <summary>
    /// Gets or sets the weekly export time. Only the hour and minute components are persisted.
    /// </summary>
    public DateTime? WeeklyExportTime
    {
        get => GetProperty(() => WeeklyExportTime);
        set => SetProperty(() => WeeklyExportTime, value);
    }

    /// <summary>
    /// Gets the read-only summary of the last successful weekly export run.
    /// Empty when the scheduled feature has not run yet.
    /// </summary>
    public string WeeklyExportLastRunSummary
    {
        get => GetProperty(() => WeeklyExportLastRunSummary);
        private set => SetProperty(() => WeeklyExportLastRunSummary, value);
    }

    /// <summary>
    /// Gets whether a last-run summary is available.
    /// </summary>
    public bool HasWeeklyExportLastRun => !string.IsNullOrEmpty(WeeklyExportLastRunSummary);

    /// <summary>
    /// Gets or sets the weekly export error banner text.
    /// </summary>
    public string? WeeklyExportError
    {
        get => GetProperty(() => WeeklyExportError);
        set
        {
            if (SetProperty(() => WeeklyExportError, value))
                RaisePropertyChanged(() => IsWeeklyExportErrorVisible);
        }
    }

    /// <summary>
    /// Gets whether the weekly export error banner should be visible.
    /// </summary>
    public bool IsWeeklyExportErrorVisible => !string.IsNullOrEmpty(WeeklyExportError);

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

        foreach (var toggle in StepToggles)
            toggle.IsEnabled = true;

        _logger.Debug("Settings reset to defaults");
    }

    private bool CanExecuteSave()
    {
        return !string.IsNullOrWhiteSpace(DatabasePath) && !IsBackendApiUrlInvalid;
    }

    private void ExecuteSave()
    {
        try
        {
            var enabledSteps = StepToggles.Where(t => t.IsEnabled).Select(t => t.StepKind);
            var stepNames = AboutFundCollectionStepKinds.ToNames(enabledSteps);
            // Extract hour/minute from the DateTime the TimePicker returned; date portion is ignored.
            var pickedTimeOfDay = AutoStartTime?.TimeOfDay ?? new TimeSpan(20, 0, 0);
            var autoStartTime = AutoStartEnabled
                ? new TimeSpan(pickedTimeOfDay.Hours, pickedTimeOfDay.Minutes, 0)
                : (TimeSpan?)null;

            _logger.Info(
                "Saving settings - Provider: {0}, DB path: {1}, AutoStart: {2} at {3:hh\\:mm}",
                SelectedProvider.Provider, DatabasePath, AutoStartEnabled, autoStartTime ?? TimeSpan.Zero);

            var pickedWeeklyTimeOfDay = WeeklyExportTime?.TimeOfDay ?? new TimeSpan(22, 0, 0);
            var weeklyExportTime = WeeklyExportEnabled
                ? new TimeSpan(pickedWeeklyTimeOfDay.Hours, pickedWeeklyTimeOfDay.Minutes, 0)
                : (TimeSpan?)null;

            var settings = new UserSettings
            {
                DatabaseProvider = SelectedProvider.Provider,
                DatabasePath = DatabasePath,
                BackendApiUrl = string.IsNullOrWhiteSpace(BackendApiUrl) ? null : BackendApiUrl.TrimEnd('/'),
                BackendApiKey = string.IsNullOrWhiteSpace(BackendApiKey) ? null : BackendApiKey,
                EnabledCrawlerSteps = stepNames,
                AutoStartEnabled = AutoStartEnabled,
                AutoStartTimeOfDay = autoStartTime,
                AutoStartPassAutoListFlag = AutoStartPassAutoListFlag,
                WeeklyExportEnabled = WeeklyExportEnabled,
                WeeklyExportDay = WeeklyExportEnabled ? WeeklyExportDay : _userSettings.WeeklyExportDay,
                WeeklyExportTimeOfDay = weeklyExportTime,
                WeeklyExportLastRunAt = _userSettings.WeeklyExportLastRunAt,
                WeeklyExportLastRunRowCount = _userSettings.WeeklyExportLastRunRowCount,
                StatsExportWindowDays = _userSettings.StatsExportWindowDays,
                StatsExportLookbackDays = _userSettings.StatsExportLookbackDays,
                StatsExportMinOwners = _userSettings.StatsExportMinOwners,
                StatsExportCompanyFilter = _userSettings.StatsExportCompanyFilter,
                StatsExportOutputPath = _userSettings.StatsExportOutputPath,
                StatsExportMetadataOutputPath = _userSettings.StatsExportMetadataOutputPath
            };

            // Persist to disk BEFORE touching Task Scheduler — if the scheduler call fails and we
            // restart as admin, the elevated instance must find the user's pending values on disk.
            _settingsService.Save(settings);

            // Sync DI singleton so future AboutFund windows pick up new defaults without restart
            _userSettings.EnabledCrawlerSteps = stepNames;
            _userSettings.AutoStartEnabled = AutoStartEnabled;
            _userSettings.AutoStartTimeOfDay = autoStartTime;
            _userSettings.AutoStartPassAutoListFlag = AutoStartPassAutoListFlag;
            _userSettings.WeeklyExportEnabled = WeeklyExportEnabled;
            _userSettings.WeeklyExportDay = settings.WeeklyExportDay;
            _userSettings.WeeklyExportTimeOfDay = weeklyExportTime;

            if (!TryApplyAutoStartSchedule(autoStartTime))
                return; // error banner already set, keep window open

            if (!TryApplyWeeklyExportSchedule(WeeklyExportDay, weeklyExportTime))
                return;

            _logger.Info("Settings saved successfully");
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings");
        }
    }

    private bool TryApplyWeeklyExportSchedule(DayOfWeek day, TimeSpan? timeOfDay)
    {
        try
        {
            if (WeeklyExportEnabled && timeOfDay.HasValue)
                _autoStartScheduler.EnableWeeklyStatsExport(day, timeOfDay.Value);
            else
                _autoStartScheduler.DisableWeeklyStatsExport();

            WeeklyExportError = null;
            return true;
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            _logger.Warn(ex, "Access denied updating weekly export task — prompting for elevation");

            var result = MessageBox.Show(
                "Windows denied creating the weekly statistics export scheduled task.\n\n" +
                "Would you like to restart YieldRaccoon as administrator to try again?\n" +
                "Your settings have already been saved — the elevated instance will reopen this window " +
                "so you can click Save again.",
                "Administrator rights required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes && TryRestartElevated())
                return false;

            WeeklyExportError = "Access denied. Run YieldRaccoon as administrator to enable weekly export.";
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update weekly export scheduled task");
            WeeklyExportError = $"Failed to update scheduled task: {ex.Message}";
            return false;
        }
    }

    private void UpdateWeeklyExportSummary()
    {
        if (_userSettings?.WeeklyExportLastRunAt is null)
        {
            WeeklyExportLastRunSummary = string.Empty;
        }
        else
        {
            var ran = _userSettings.WeeklyExportLastRunAt.Value;
            var rows = _userSettings.WeeklyExportLastRunRowCount;
            WeeklyExportLastRunSummary = rows.HasValue
                ? $"Last run: {ran:yyyy-MM-dd HH:mm} — {rows.Value:N0} rows"
                : $"Last run: {ran:yyyy-MM-dd HH:mm}";
        }
        RaisePropertyChanged(() => HasWeeklyExportLastRun);
    }

    private bool TryApplyAutoStartSchedule(TimeSpan? autoStartTime)
    {
        try
        {
            if (AutoStartEnabled && autoStartTime.HasValue)
                _autoStartScheduler.EnableDaily(autoStartTime.Value, AutoStartPassAutoListFlag);
            else
                _autoStartScheduler.Disable();

            AutoStartError = null;
            return true;
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            _logger.Warn(ex, "Access denied updating scheduled task — prompting for elevation");

            var result = MessageBox.Show(
                "Windows denied creating the auto-start scheduled task.\n\n" +
                "Would you like to restart YieldRaccoon as administrator to try again?\n" +
                "Your settings have already been saved — the elevated instance will reopen this window " +
                "so you can click Save again.",
                "Administrator rights required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes && TryRestartElevated())
                return false; // current instance is shutting down

            AutoStartError = "Access denied. Run YieldRaccoon as administrator to enable auto-start.";
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update auto-start scheduled task");
            AutoStartError = $"Failed to update scheduled task: {ex.Message}";
            return false;
        }
    }

    private bool TryRestartElevated()
    {
        try
        {
            var exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Unable to resolve current process path.");

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--elevated-settings",
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(psi);
            _logger.Info("Elevated instance started, shutting down current instance");
            System.Windows.Application.Current.Shutdown();
            return true;
        }
        catch (Win32Exception ex)
        {
            // User cancelled the UAC prompt — ERROR_CANCELLED (1223)
            _logger.Info(ex, "UAC elevation cancelled by user");
            AutoStartError = "Elevation cancelled. Auto-start not enabled.";
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to restart as administrator");
            AutoStartError = $"Failed to restart as administrator: {ex.Message}";
            return false;
        }
    }

    private static bool IsAccessDenied(Exception ex) =>
        ex is UnauthorizedAccessException
        || (ex is COMException cex && cex.HResult == HResultAccessDenied);

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

    private static bool IsValidAbsoluteUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https";

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
