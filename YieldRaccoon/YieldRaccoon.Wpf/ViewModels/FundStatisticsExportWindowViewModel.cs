using System.IO;
using System.Windows.Input;
using DevExpress.Mvvm;
using Microsoft.Win32;
using NLog;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Models;
using YieldRaccoon.Wpf.Services;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Fund Statistics Export window.
/// Computes summary statistics per fund per time window and exports as CSV.
/// </summary>
public class FundStatisticsExportWindowViewModel : ViewModelBase
{
    private const int DefaultMinNumberOfOwners = 100;
    private const int DefaultWindowDays = 14;
    private const int DefaultLookbackDays = 365;

    private readonly ILogger _logger;
    private readonly IFundStatisticsCsvExportService _exportService;
    private readonly IFundMetadataCsvExportService _metadataExportService;
    private readonly DatabaseOptions _databaseOptions;
    private readonly IUserSettingsService? _userSettingsService;
    private readonly UserSettings? _userSettings;
    private readonly AutoStartOptions? _autoStartOptions;
    private readonly string _sourceDirectory;
    private bool _scheduledAutoRunTriggered;

    /// <summary>
    /// Gets the current window service for programmatic close.
    /// </summary>
    protected ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();

    /// <summary>
    /// Initializes a new instance of the <see cref="FundStatisticsExportWindowViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="exportService">Service for computing and exporting fund statistics.</param>
    /// <param name="metadataExportService">Service for exporting fund profile metadata.</param>
    /// <param name="databaseOptions">Current database configuration.</param>
    /// <param name="userSettingsService">Persists last-used export values back to disk on successful export.</param>
    /// <param name="userSettings">Current user settings — pre-populates fields on open.</param>
    /// <param name="autoStartOptions">CLI options — <c>--auto-weekly-stats</c> flips this VM into scheduled-run mode.</param>
    public FundStatisticsExportWindowViewModel(
        ILogger logger,
        IFundStatisticsCsvExportService exportService,
        IFundMetadataCsvExportService metadataExportService,
        DatabaseOptions databaseOptions,
        IUserSettingsService userSettingsService,
        UserSettings userSettings,
        AutoStartOptions autoStartOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _metadataExportService = metadataExportService ?? throw new ArgumentNullException(nameof(metadataExportService));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));
        _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        _userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
        _autoStartOptions = autoStartOptions ?? throw new ArgumentNullException(nameof(autoStartOptions));

        IsSqliteProvider = databaseOptions.Provider is DatabaseProvider.SQLite or DatabaseProvider.DualWrite;
        _sourceDirectory = GetSourceDirectory(databaseOptions.ConnectionString);
        Periods = CreatePeriods();
        LookbackPeriods = CreateLookbackPeriods();

        // Pre-populate from persisted StatsExport* values when present; otherwise the defaults:
        // 2 weeks window, 1 year lookback, 100 min owners.
        SelectedPeriod = FindPeriodOrDefault(Periods, userSettings.StatsExportWindowDays, DefaultWindowDays);
        SelectedLookbackPeriod = FindPeriodOrDefault(LookbackPeriods, userSettings.StatsExportLookbackDays, DefaultLookbackDays);
        MinNumberOfOwners = userSettings.StatsExportMinOwners ?? DefaultMinNumberOfOwners;
        CompanyName = userSettings.StatsExportCompanyFilter ?? string.Empty;
        OutputPath = !string.IsNullOrWhiteSpace(userSettings.StatsExportOutputPath)
            ? userSettings.StatsExportOutputPath
            : BuildDefaultPath(CompanyName, SelectedPeriod, SelectedLookbackPeriod);
        MetadataOutputPath = !string.IsNullOrWhiteSpace(userSettings.StatsExportMetadataOutputPath)
            ? userSettings.StatsExportMetadataOutputPath
            : BuildDefaultMetadataPath(CompanyName);

        IsExporting = false;
        StatusMessage = string.Empty;
        IsStatusError = false;
        ProgressValue = 0;
        ProgressText = string.Empty;

        ExportCommand = new DelegateCommand(ExecuteExport, CanExecuteExport, true);
        BrowseCommand = new DelegateCommand(ExecuteBrowse, CanExecuteBrowse, true);
        BrowseMetadataCommand = new DelegateCommand(ExecuteBrowseMetadata, CanExecuteBrowse, true);
        CloseCommand = new DelegateCommand(ExecuteClose);
        WindowClosingCommand = new DelegateCommand(ExecuteWindowClosing);
        LoadedCommand = new DelegateCommand(ExecuteLoaded);

        _logger.Debug("FundStatisticsExportWindowViewModel initialized, IsSqliteProvider={0}", IsSqliteProvider);
    }

    /// <summary>
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public FundStatisticsExportWindowViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _exportService = null!;
        _metadataExportService = null!;
        _databaseOptions = new DatabaseOptions();
        _userSettingsService = null;
        _userSettings = null;
        _autoStartOptions = null;
        _sourceDirectory = string.Empty;

        IsSqliteProvider = true;
        Periods = CreatePeriods();
        SelectedPeriod = Periods[1];
        LookbackPeriods = CreateLookbackPeriods();
        SelectedLookbackPeriod = LookbackPeriods[4]; // 1 year
        CompanyName = string.Empty;
        OutputPath = @"YieldRaccoon_summary_2weeks_1year.csv";
        MetadataOutputPath = @"YieldRaccoon_metadata.csv";
        MinNumberOfOwners = DefaultMinNumberOfOwners;
        IsExporting = false;
        StatusMessage = string.Empty;
        IsStatusError = false;
        ProgressValue = 0;
        ProgressText = string.Empty;

        ExportCommand = new DelegateCommand(() => { });
        BrowseCommand = new DelegateCommand(() => { });
        BrowseMetadataCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
        WindowClosingCommand = new DelegateCommand(() => { });
        LoadedCommand = new DelegateCommand(() => { });
    }

    #region Properties

    /// <summary>
    /// Gets the available window size periods.
    /// </summary>
    public IReadOnlyList<ExportPeriod> Periods { get; }

    /// <summary>
    /// Gets or sets the selected window size period.
    /// </summary>
    public ExportPeriod SelectedPeriod
    {
        get => GetProperty(() => SelectedPeriod);
        set
        {
            if (SetProperty(() => SelectedPeriod, value))
                UpdateDefaultFilename();
        }
    }

    /// <summary>
    /// Gets the available lookback periods (how far back in NAV history to go).
    /// </summary>
    public IReadOnlyList<ExportPeriod> LookbackPeriods { get; }

    /// <summary>
    /// Gets or sets the selected lookback period.
    /// </summary>
    public ExportPeriod SelectedLookbackPeriod
    {
        get => GetProperty(() => SelectedLookbackPeriod);
        set
        {
            if (SetProperty(() => SelectedLookbackPeriod, value))
                UpdateDefaultFilename();
        }
    }

    /// <summary>
    /// Gets or sets the company name to filter by.
    /// </summary>
    public string CompanyName
    {
        get => GetProperty(() => CompanyName);
        set
        {
            if (SetProperty(() => CompanyName, value))
                UpdateDefaultFilename();
        }
    }

    /// <summary>
    /// Gets or sets the output CSV file path.
    /// </summary>
    public string OutputPath
    {
        get => GetProperty(() => OutputPath);
        set => SetProperty(() => OutputPath, value);
    }

    /// <summary>
    /// Gets or sets the metadata CSV output file path.
    /// </summary>
    public string MetadataOutputPath
    {
        get => GetProperty(() => MetadataOutputPath);
        set => SetProperty(() => MetadataOutputPath, value);
    }

    /// <summary>
    /// Gets or sets whether an export operation is in progress.
    /// </summary>
    public bool IsExporting
    {
        get => GetProperty(() => IsExporting);
        set => SetProperty(() => IsExporting, value);
    }

    /// <summary>
    /// Gets whether the SQLite database provider is active.
    /// </summary>
    public bool IsSqliteProvider { get; }

    /// <summary>
    /// Gets or sets the status message displayed after export.
    /// </summary>
    public string StatusMessage
    {
        get => GetProperty(() => StatusMessage);
        set => SetProperty(() => StatusMessage, value);
    }

    /// <summary>
    /// Gets or sets the minimum number of owners a fund must have to be included.
    /// </summary>
    public int MinNumberOfOwners
    {
        get => GetProperty(() => MinNumberOfOwners);
        set => SetProperty(() => MinNumberOfOwners, value);
    }

    /// <summary>
    /// Gets or sets whether the status message indicates an error.
    /// </summary>
    public bool IsStatusError
    {
        get => GetProperty(() => IsStatusError);
        set => SetProperty(() => IsStatusError, value);
    }

    /// <summary>
    /// Gets or sets the export progress percentage (0–100).
    /// </summary>
    public int ProgressValue
    {
        get => GetProperty(() => ProgressValue);
        set => SetProperty(() => ProgressValue, value);
    }

    /// <summary>
    /// Gets or sets the progress text (e.g., "Processing fund 150 of 1400...").
    /// </summary>
    public string ProgressText
    {
        get => GetProperty(() => ProgressText);
        set => SetProperty(() => ProgressText, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to execute the statistics export.
    /// </summary>
    public ICommand ExportCommand { get; }

    /// <summary>
    /// Gets the command to browse for an output file location.
    /// </summary>
    public ICommand BrowseCommand { get; }

    /// <summary>
    /// Gets the command to browse for a metadata output file location.
    /// </summary>
    public ICommand BrowseMetadataCommand { get; }

    /// <summary>
    /// Gets the command to close the window.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Gets the command executed when the window is closing.
    /// </summary>
    public ICommand WindowClosingCommand { get; }

    /// <summary>
    /// Gets the command executed when the window is loaded. Used to kick off the scheduled
    /// auto-run when the app was launched with <c>--auto-weekly-stats</c>.
    /// </summary>
    public ICommand LoadedCommand { get; }

    #endregion

    #region Command Implementations

    private bool CanExecuteExport()
    {
        return IsSqliteProvider
               && !IsExporting
               && !string.IsNullOrWhiteSpace(OutputPath)
               && !string.IsNullOrWhiteSpace(MetadataOutputPath);
    }

    private async void ExecuteExport()
    {
        _logger.Info("Statistics export started: company={0}, window={1}, lookback={2}", CompanyName, SelectedPeriod.DisplayName, SelectedLookbackPeriod.DisplayName);

        IsExporting = true;
        StatusMessage = string.Empty;
        IsStatusError = false;
        ProgressValue = 0;
        ProgressText = "Starting...";

        // Scheduled weekly runs always append a date suffix so each week's snapshot is preserved
        // instead of overwriting the previous one. Manual runs keep the user-picked filename.
        var isScheduledRun = _autoStartOptions?.AutoWeeklyStats == true;
        var statsOutputPath = isScheduledRun ? AppendDateSuffix(OutputPath) : OutputPath;
        var metadataOutputPath = isScheduledRun ? AppendDateSuffix(MetadataOutputPath) : MetadataOutputPath;

        try
        {
            var sourcePath = ExtractDatabasePath(_databaseOptions.ConnectionString);
            var companyFilter = string.IsNullOrWhiteSpace(CompanyName) ? null : CompanyName.Trim();
            var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-SelectedLookbackPeriod.Days));

            var progress = new Progress<(int processed, int total)>(p =>
            {
                ProgressValue = p.total > 0 ? (int)(100.0 * p.processed / p.total) : 0;
                ProgressText = $"Processing fund {p.processed} of {p.total}...";
            });

            var rowCount = await _exportService.ExportAsync(
                sourcePath,
                statsOutputPath,
                SelectedPeriod.Days,
                companyFilter,
                MinNumberOfOwners,
                cutoffDate,
                progress);

            // Metadata export
            ProgressText = "Writing metadata...";
            var metadataRowCount = await _metadataExportService.ExportAsync(
                sourcePath,
                metadataOutputPath,
                companyFilter,
                MinNumberOfOwners);

            StatusMessage = $"Exported {rowCount} stat rows + {metadataRowCount} metadata rows";
            IsStatusError = false;
            _logger.Info("Export completed: stats={0} ({1} rows), metadata={2} ({3} rows)",
                statsOutputPath, rowCount, metadataOutputPath, metadataRowCount);

            PersistSuccessfulRun(rowCount, isScheduledRun);

            if (isScheduledRun)
            {
                _logger.Info("Scheduled weekly stats run completed; closing window");
                CurrentWindowService?.Close();
            }
        }
        catch (FileNotFoundException ex)
        {
            StatusMessage = $"Source database not found: {ex.FileName}";
            IsStatusError = true;
            _logger.Error(ex, "Statistics export failed — source not found");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            IsStatusError = true;
            _logger.Error(ex, "Statistics export failed");
        }
        finally
        {
            IsExporting = false;
        }
    }

    /// <summary>
    /// Window Loaded handler. When the app was launched with <c>--auto-weekly-stats</c> and the
    /// Export command has not run yet for this VM instance, invokes it automatically.
    /// </summary>
    private void ExecuteLoaded()
    {
        if (_scheduledAutoRunTriggered)
            return;
        if (_autoStartOptions?.AutoWeeklyStats != true)
            return;
        if (!IsSqliteProvider)
        {
            _logger.Warn("Scheduled weekly stats run requested but provider is not SQLite — skipping");
            return;
        }

        _scheduledAutoRunTriggered = true;
        _logger.Info("Auto-triggering Export command for scheduled weekly stats run");
        if (ExportCommand.CanExecute(null))
            ExportCommand.Execute(null);
    }

    private void PersistSuccessfulRun(int rowCount, bool isScheduledRun)
    {
        if (_userSettingsService is null || _userSettings is null)
            return;

        try
        {
            _userSettings.StatsExportWindowDays = SelectedPeriod.Days;
            _userSettings.StatsExportLookbackDays = SelectedLookbackPeriod.Days;
            _userSettings.StatsExportMinOwners = MinNumberOfOwners;
            _userSettings.StatsExportCompanyFilter = string.IsNullOrWhiteSpace(CompanyName) ? null : CompanyName.Trim();
            _userSettings.StatsExportOutputPath = OutputPath;
            _userSettings.StatsExportMetadataOutputPath = MetadataOutputPath;

            if (isScheduledRun)
            {
                _userSettings.WeeklyExportLastRunAt = DateTime.Now;
                _userSettings.WeeklyExportLastRunRowCount = rowCount;
            }

            _userSettingsService.Save(_userSettings);
            _logger.Debug("Persisted stats export settings (scheduled={0}, rows={1})", isScheduledRun, rowCount);
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Failed to persist stats export settings — continuing without");
        }
    }

    private static string AppendDateSuffix(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var dateTag = DateTime.Now.ToString("yyyy-MM-dd");
        var stamped = $"{nameWithoutExt}_{dateTag}{extension}";
        return string.IsNullOrEmpty(directory) ? stamped : Path.Combine(directory, stamped);
    }

    private bool CanExecuteBrowse() => !IsExporting;

    private void ExecuteBrowse()
    {
        _logger.Debug("Browse for statistics export output path");

        var dialog = new SaveFileDialog
        {
            Title = "Select export location",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = Path.GetFileName(OutputPath),
            InitialDirectory = GetInitialDirectory(),
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.FileName;
            _logger.Info("Selected statistics export path: {0}", OutputPath);
        }
    }

    private void ExecuteBrowseMetadata()
    {
        _logger.Debug("Browse for metadata export output path");

        var dialog = new SaveFileDialog
        {
            Title = "Select metadata export location",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = Path.GetFileName(MetadataOutputPath),
            InitialDirectory = GetInitialDirectory(),
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            MetadataOutputPath = dialog.FileName;
            _logger.Info("Selected metadata export path: {0}", MetadataOutputPath);
        }
    }

    private void ExecuteClose()
    {
        _logger.Debug("Statistics export window close requested");
        CurrentWindowService?.Close();
    }

    private void ExecuteWindowClosing()
    {
        _logger.Debug("Statistics export window closing");
    }

    #endregion

    #region Helpers

    private static IReadOnlyList<ExportPeriod> CreatePeriods() =>
    [
        new ExportPeriod("1 week", 7),
        new ExportPeriod("2 weeks", 14),
        new ExportPeriod("3 weeks", 21),
        new ExportPeriod("1 month", 30),
        new ExportPeriod("3 months", 90)
    ];

    private static IReadOnlyList<ExportPeriod> CreateLookbackPeriods() =>
    [
        new ExportPeriod("1 month", 30),
        new ExportPeriod("2 months", 60),
        new ExportPeriod("3 months", 90),
        new ExportPeriod("6 months", 180),
        new ExportPeriod("1 year", 365)
    ];

    private static ExportPeriod FindPeriodOrDefault(
        IReadOnlyList<ExportPeriod> periods, int? requestedDays, int fallbackDays)
    {
        var target = requestedDays ?? fallbackDays;
        return periods.FirstOrDefault(p => p.Days == target)
               ?? periods.FirstOrDefault(p => p.Days == fallbackDays)
               ?? periods[0];
    }

    private void UpdateDefaultFilename()
    {
        if (SelectedPeriod == null || SelectedLookbackPeriod == null)
            return;

        OutputPath = BuildDefaultPath(CompanyName, SelectedPeriod, SelectedLookbackPeriod);
        MetadataOutputPath = BuildDefaultMetadataPath(CompanyName);
    }

    private string BuildDefaultPath(string companyName, ExportPeriod period, ExportPeriod lookback)
    {
        var periodTag = period.DisplayName.Replace(" ", "");
        var lookbackTag = lookback.DisplayName.Replace(" ", "");
        var filename = string.IsNullOrWhiteSpace(companyName)
            ? $"YieldRaccoon_summary_{periodTag}_{lookbackTag}.csv"
            : $"YieldRaccoon_summary_{SanitizeFilename(companyName.Trim())}_{periodTag}_{lookbackTag}.csv";

        return string.IsNullOrEmpty(_sourceDirectory)
            ? filename
            : Path.Combine(_sourceDirectory, filename);
    }

    private string BuildDefaultMetadataPath(string companyName)
    {
        var filename = string.IsNullOrWhiteSpace(companyName)
            ? "YieldRaccoon_metadata.csv"
            : $"YieldRaccoon_metadata_{SanitizeFilename(companyName.Trim())}.csv";

        return string.IsNullOrEmpty(_sourceDirectory)
            ? filename
            : Path.Combine(_sourceDirectory, filename);
    }

    private static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray());
        return sanitized.Replace(' ', '_');
    }

    private string GetInitialDirectory()
    {
        if (!string.IsNullOrEmpty(_sourceDirectory) && Directory.Exists(_sourceDirectory))
            return _sourceDirectory;

        try
        {
            var directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                return directory;
        }
        catch
        {
            // Ignore path errors
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private static string GetSourceDirectory(string connectionString)
    {
        try
        {
            var dbPath = ExtractDatabasePath(connectionString);
            var fullPath = Path.GetFullPath(dbPath);
            return Path.GetDirectoryName(fullPath) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Extracts the database file path from a SQLite connection string.
    /// </summary>
    private static string ExtractDatabasePath(string connectionString)
    {
        const string dataSourcePrefix = "Data Source=";

        if (string.IsNullOrWhiteSpace(connectionString))
            return DatabaseOptions.DefaultDatabaseFileName;

        var index = connectionString.IndexOf(dataSourcePrefix, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            var path = connectionString[(index + dataSourcePrefix.Length)..].Trim();
            var semicolonIndex = path.IndexOf(';');
            if (semicolonIndex >= 0)
                path = path[..semicolonIndex];
            return path;
        }

        return connectionString;
    }

    #endregion
}
