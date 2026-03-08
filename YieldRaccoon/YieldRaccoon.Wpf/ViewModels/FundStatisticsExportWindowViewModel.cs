using System.IO;
using System.Windows.Input;
using DevExpress.Mvvm;
using Microsoft.Win32;
using NLog;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Fund Statistics Export window.
/// Computes summary statistics per fund per time window and exports as CSV.
/// </summary>
public class FundStatisticsExportWindowViewModel : ViewModelBase
{
    private const int DefaultMinNumberOfOwners = 100;

    private readonly ILogger _logger;
    private readonly IFundStatisticsCsvExportService _exportService;
    private readonly IFundMetadataCsvExportService _metadataExportService;
    private readonly DatabaseOptions _databaseOptions;
    private readonly string _sourceDirectory;

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
    public FundStatisticsExportWindowViewModel(
        ILogger logger,
        IFundStatisticsCsvExportService exportService,
        IFundMetadataCsvExportService metadataExportService,
        DatabaseOptions databaseOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _metadataExportService = metadataExportService ?? throw new ArgumentNullException(nameof(metadataExportService));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));

        IsSqliteProvider = databaseOptions.Provider is DatabaseProvider.SQLite or DatabaseProvider.DualWrite;
        _sourceDirectory = GetSourceDirectory(databaseOptions.ConnectionString);
        Periods = CreatePeriods();
        SelectedPeriod = Periods[1]; // 2 weeks default
        LookbackPeriods = CreateLookbackPeriods();
        SelectedLookbackPeriod = LookbackPeriods[3]; // 6 months default
        CompanyName = string.Empty;
        OutputPath = BuildDefaultPath(string.Empty, Periods[1], LookbackPeriods[3]);
        MetadataOutputPath = BuildDefaultMetadataPath(string.Empty);
        MinNumberOfOwners = DefaultMinNumberOfOwners;
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
        _sourceDirectory = string.Empty;

        IsSqliteProvider = true;
        Periods = CreatePeriods();
        SelectedPeriod = Periods[1];
        LookbackPeriods = CreateLookbackPeriods();
        SelectedLookbackPeriod = LookbackPeriods[3];
        CompanyName = string.Empty;
        OutputPath = @"YieldRaccoon_stats_2weeks_6months.csv";
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
                OutputPath,
                SelectedPeriod.Days,
                companyFilter,
                MinNumberOfOwners,
                cutoffDate,
                progress);

            // Metadata export
            ProgressText = "Writing metadata...";
            var metadataRowCount = await _metadataExportService.ExportAsync(
                sourcePath,
                MetadataOutputPath,
                companyFilter,
                MinNumberOfOwners);

            StatusMessage = $"Exported {rowCount} stat rows + {metadataRowCount} metadata rows";
            IsStatusError = false;
            _logger.Info("Export completed: stats={0} ({1} rows), metadata={2} ({3} rows)",
                OutputPath, rowCount, MetadataOutputPath, metadataRowCount);
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
            ? $"YieldRaccoon_stats_{periodTag}_{lookbackTag}.csv"
            : $"YieldRaccoon_stats_{SanitizeFilename(companyName.Trim())}_{periodTag}_{lookbackTag}.csv";

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
