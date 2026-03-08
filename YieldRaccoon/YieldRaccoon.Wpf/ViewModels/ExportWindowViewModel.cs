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
/// ViewModel for the Export window.
/// Allows users to export filtered fund data to a standalone SQLite database file.
/// </summary>
public class ExportWindowViewModel : ViewModelBase
{
    private const int DefaultMinNumberOfOwners = 100;

    private readonly ILogger _logger;
    private readonly IFundDataExportService _exportService;
    private readonly DatabaseOptions _databaseOptions;
    private readonly string _sourceDirectory;

    /// <summary>
    /// Gets the current window service for programmatic close.
    /// </summary>
    protected ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportWindowViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="exportService">Service for exporting filtered fund data.</param>
    /// <param name="databaseOptions">Current database configuration.</param>
    public ExportWindowViewModel(
        ILogger logger,
        IFundDataExportService exportService,
        DatabaseOptions databaseOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));

        IsSqliteProvider = databaseOptions.Provider is DatabaseProvider.SQLite or DatabaseProvider.DualWrite;
        _sourceDirectory = GetSourceDirectory(databaseOptions.ConnectionString);
        Periods = CreatePeriods();
        SelectedPeriod = Periods[1];
        CompanyName = string.Empty;
        OutputPath = BuildDefaultPath(string.Empty, Periods[1]);
        MinNumberOfOwners = DefaultMinNumberOfOwners;
        IsExporting = false;
        StatusMessage = string.Empty;
        IsStatusError = false;

        ExportCommand = new DelegateCommand(ExecuteExport, CanExecuteExport, true);
        BrowseCommand = new DelegateCommand(ExecuteBrowse, CanExecuteBrowse, true);
        CloseCommand = new DelegateCommand(ExecuteClose);
        WindowClosingCommand = new DelegateCommand(ExecuteWindowClosing);

        _logger.Debug("ExportWindowViewModel initialized, IsSqliteProvider={0}", IsSqliteProvider);
    }

    /// <summary>
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public ExportWindowViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _exportService = null!;
        _databaseOptions = new DatabaseOptions();
        _sourceDirectory = string.Empty;

        IsSqliteProvider = true;
        Periods = CreatePeriods();
        SelectedPeriod = Periods[1];
        CompanyName = "Handelsbanken";
        OutputPath = @"YieldRaccoon_Handelsbanken_1week.db";
        MinNumberOfOwners = DefaultMinNumberOfOwners;
        IsExporting = false;
        StatusMessage = string.Empty;
        IsStatusError = false;

        ExportCommand = new DelegateCommand(() => { });
        BrowseCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
        WindowClosingCommand = new DelegateCommand(() => { });
    }

    #region Properties

    /// <summary>
    /// Gets the available export time periods.
    /// </summary>
    public IReadOnlyList<ExportPeriod> Periods { get; }

    /// <summary>
    /// Gets or sets the selected time period.
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
    /// Gets or sets the output database file path.
    /// </summary>
    public string OutputPath
    {
        get => GetProperty(() => OutputPath);
        set => SetProperty(() => OutputPath, value);
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
    /// Gets or sets the minimum number of owners a fund must have to be included in the export.
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

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to execute the export operation.
    /// </summary>
    public ICommand ExportCommand { get; }

    /// <summary>
    /// Gets the command to browse for an output file location.
    /// </summary>
    public ICommand BrowseCommand { get; }

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
               && !string.IsNullOrWhiteSpace(OutputPath);
    }

    private async void ExecuteExport()
    {
        _logger.Info("Export started: company={0}, period={1}", CompanyName, SelectedPeriod.DisplayName);

        IsExporting = true;
        StatusMessage = string.Empty;
        IsStatusError = false;

        try
        {
            var sourcePath = ExtractDatabasePath(_databaseOptions.ConnectionString);
            var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-SelectedPeriod.Days));

            var companyFilter = string.IsNullOrWhiteSpace(CompanyName) ? null : CompanyName.Trim();
            await _exportService.ExportAsync(sourcePath, OutputPath, companyFilter, cutoffDate, MinNumberOfOwners);

            StatusMessage = $"Exported successfully to {Path.GetFileName(OutputPath)}";
            IsStatusError = false;
            _logger.Info("Export completed: {0}", OutputPath);
        }
        catch (FileNotFoundException ex)
        {
            StatusMessage = $"Source database not found: {ex.FileName}";
            IsStatusError = true;
            _logger.Error(ex, "Export failed — source not found");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            IsStatusError = true;
            _logger.Error(ex, "Export failed");
        }
        finally
        {
            IsExporting = false;
        }
    }

    private bool CanExecuteBrowse() => !IsExporting;

    private void ExecuteBrowse()
    {
        _logger.Debug("Browse for export output path");

        var dialog = new SaveFileDialog
        {
            Title = "Select export location",
            Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
            DefaultExt = ".db",
            FileName = Path.GetFileName(OutputPath),
            InitialDirectory = GetInitialDirectory(),
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.FileName;
            _logger.Info("Selected export path: {0}", OutputPath);
        }
    }

    private void ExecuteClose()
    {
        _logger.Debug("Export window close requested");
        CurrentWindowService?.Close();
    }

    private void ExecuteWindowClosing()
    {
        _logger.Debug("Export window closing");
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

    private void UpdateDefaultFilename()
    {
        if (SelectedPeriod == null)
            return;

        OutputPath = BuildDefaultPath(CompanyName, SelectedPeriod);
    }

    private string BuildDefaultPath(string companyName, ExportPeriod period)
    {
        var periodTag = period.DisplayName.Replace(" ", "");
        var filename = string.IsNullOrWhiteSpace(companyName)
            ? $"YieldRaccoon_{periodTag}.db"
            : $"YieldRaccoon_{SanitizeFilename(companyName.Trim())}_{periodTag}.db";

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
