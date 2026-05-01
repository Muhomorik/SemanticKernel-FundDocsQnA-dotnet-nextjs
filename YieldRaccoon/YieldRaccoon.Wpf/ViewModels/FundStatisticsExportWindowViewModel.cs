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
/// Coordinates the per-bucket summary CSV, the rolling-horizon snapshot CSV, and the metadata CSV —
/// all written under the same ISO-week-tagged filename family so a "week bundle" matches by glob.
/// </summary>
public class FundStatisticsExportWindowViewModel : ViewModelBase
{
    private const int DefaultMinNumberOfOwners = 100;
    private const int DefaultWindowDays = 14;
    private const int DefaultLookbackDays = 365;

    private readonly ILogger _logger;
    private readonly IFundStatisticsCsvExportService _exportService;
    private readonly IFundMetadataCsvExportService _metadataExportService;
    private readonly IFundSnapshotCsvExportService _snapshotExportService;
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
    public FundStatisticsExportWindowViewModel(
        ILogger logger,
        IFundStatisticsCsvExportService exportService,
        IFundMetadataCsvExportService metadataExportService,
        IFundSnapshotCsvExportService snapshotExportService,
        DatabaseOptions databaseOptions,
        IUserSettingsService userSettingsService,
        UserSettings userSettings,
        AutoStartOptions autoStartOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _metadataExportService = metadataExportService ?? throw new ArgumentNullException(nameof(metadataExportService));
        _snapshotExportService = snapshotExportService ?? throw new ArgumentNullException(nameof(snapshotExportService));
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

        // Default paths are always rebuilt from CompanyName + current ISO week. The persisted
        // OutputPath / SnapshotOutputPath / MetadataOutputPath properties on UserSettings are
        // intentionally ignored — they were per-run customizations from v1 with stale ISO weeks
        // and the legacy "_2weeks_1year" tag baked in. Browse-button overrides remain session-scoped.
        OutputPath = BuildDefaultPath("summary", CompanyName);
        SnapshotOutputPath = BuildDefaultPath("snapshot", CompanyName);
        MetadataOutputPath = BuildDefaultPath("metadata", CompanyName);

        IsExporting = false;
        StatusMessage = string.Empty;
        IsStatusError = false;
        ProgressValue = 0;
        ProgressText = string.Empty;

        ExportCommand = new DelegateCommand(ExecuteExport, CanExecuteExport, true);
        BrowseCommand = new DelegateCommand(ExecuteBrowse, CanExecuteBrowse, true);
        BrowseSnapshotCommand = new DelegateCommand(ExecuteBrowseSnapshot, CanExecuteBrowse, true);
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
        _snapshotExportService = null!;
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
        OutputPath = "YieldRaccoon_summary_all_2026-W18.csv";
        SnapshotOutputPath = "YieldRaccoon_snapshot_all_2026-W18.csv";
        MetadataOutputPath = "YieldRaccoon_metadata_all_2026-W18.csv";
        MinNumberOfOwners = DefaultMinNumberOfOwners;
        IsExporting = false;
        StatusMessage = string.Empty;
        IsStatusError = false;
        ProgressValue = 0;
        ProgressText = string.Empty;

        ExportCommand = new DelegateCommand(() => { });
        BrowseCommand = new DelegateCommand(() => { });
        BrowseSnapshotCommand = new DelegateCommand(() => { });
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
        set => SetProperty(() => SelectedPeriod, value);
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
        set => SetProperty(() => SelectedLookbackPeriod, value);
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
                UpdateDefaultFilenames();
        }
    }

    /// <summary>
    /// Gets or sets the summary CSV output file path.
    /// </summary>
    public string OutputPath
    {
        get => GetProperty(() => OutputPath);
        set => SetProperty(() => OutputPath, value);
    }

    /// <summary>
    /// Gets or sets the snapshot CSV output file path.
    /// </summary>
    public string SnapshotOutputPath
    {
        get => GetProperty(() => SnapshotOutputPath);
        set => SetProperty(() => SnapshotOutputPath, value);
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
    /// Gets the command to browse for a summary output file location.
    /// </summary>
    public ICommand BrowseCommand { get; }

    /// <summary>
    /// Gets the command to browse for a snapshot output file location.
    /// </summary>
    public ICommand BrowseSnapshotCommand { get; }

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
               && !string.IsNullOrWhiteSpace(SnapshotOutputPath)
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

        var isScheduledRun = _autoStartOptions?.AutoWeeklyStats == true;

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

            var summaryCount = await _exportService.ExportAsync(
                sourcePath,
                OutputPath,
                SelectedPeriod.Days,
                companyFilter,
                MinNumberOfOwners,
                cutoffDate,
                progress);

            ProgressText = "Writing snapshot...";
            var snapshotCount = await _snapshotExportService.ExportAsync(
                sourcePath,
                SnapshotOutputPath,
                companyFilter,
                MinNumberOfOwners,
                progress);

            ProgressText = "Writing metadata...";
            var metadataCount = await _metadataExportService.ExportAsync(
                sourcePath,
                MetadataOutputPath,
                companyFilter,
                MinNumberOfOwners);

            StatusMessage = $"Exported {summaryCount} summary + {snapshotCount} snapshot + {metadataCount} metadata rows";
            IsStatusError = false;
            _logger.Info("Export completed: summary={0} ({1}), snapshot={2} ({3}), metadata={4} ({5})",
                OutputPath, summaryCount, SnapshotOutputPath, snapshotCount, MetadataOutputPath, metadataCount);

            PersistSuccessfulRun(summaryCount, isScheduledRun);

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

        // Singleton is shared across window lifetimes — clear after consuming so that
        // a later manual open doesn't see stale scheduled-run intent and auto-close.
        _autoStartOptions.AutoWeeklyStats = false;
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

            // Output paths are deterministic from CompanyName + current ISO week; clear any
            // stale persisted values so they don't get re-loaded on next open.
            _userSettings.StatsExportOutputPath = null;
            _userSettings.StatsExportSnapshotOutputPath = null;
            _userSettings.StatsExportMetadataOutputPath = null;

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

    private bool CanExecuteBrowse() => !IsExporting;

    private void ExecuteBrowse() => OutputPath = BrowseForCsv("Select summary export location", OutputPath) ?? OutputPath;

    private void ExecuteBrowseSnapshot() => SnapshotOutputPath = BrowseForCsv("Select snapshot export location", SnapshotOutputPath) ?? SnapshotOutputPath;

    private void ExecuteBrowseMetadata() => MetadataOutputPath = BrowseForCsv("Select metadata export location", MetadataOutputPath) ?? MetadataOutputPath;

    private string? BrowseForCsv(string title, string currentPath)
    {
        _logger.Debug("Browse: {0}", title);

        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = Path.GetFileName(currentPath),
            InitialDirectory = GetInitialDirectory(currentPath),
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            _logger.Info("Selected export path: {0}", dialog.FileName);
            return dialog.FileName;
        }

        return null;
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

    private void UpdateDefaultFilenames()
    {
        OutputPath = BuildDefaultPath("summary", CompanyName);
        SnapshotOutputPath = BuildDefaultPath("snapshot", CompanyName);
        MetadataOutputPath = BuildDefaultPath("metadata", CompanyName);
    }

    private string BuildDefaultPath(string kind, string companyName)
    {
        var family = IsoWeekFilenameBuilder.BuildFamilyTag(companyName);
        var isoWeek = IsoWeekFilenameBuilder.BuildIsoWeekTag(DateTime.Now);
        var filename = $"YieldRaccoon_{kind}_{family}_{isoWeek}.csv";

        return string.IsNullOrEmpty(_sourceDirectory)
            ? filename
            : Path.Combine(_sourceDirectory, filename);
    }

    private string GetInitialDirectory(string currentPath)
    {
        if (!string.IsNullOrEmpty(_sourceDirectory) && Directory.Exists(_sourceDirectory))
            return _sourceDirectory;

        try
        {
            var directory = Path.GetDirectoryName(currentPath);
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
