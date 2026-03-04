using System.Diagnostics;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Wpf.Configuration;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the Cloud Sync window.
/// Allows users to bulk-sync local fund data (profiles + history records) to the Backend API.
/// </summary>
public class CloudSyncWindowViewModel : ViewModelBase
{
    private const int DefaultThrottleMs = 500;

    private readonly ILogger _logger;
    private readonly ICloudSyncService _cloudSyncService;
    private readonly DatabaseOptions _databaseOptions;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Gets the current window service for programmatic close.
    /// </summary>
    protected ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudSyncWindowViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="cloudSyncService">Service for syncing fund data to the Backend API.</param>
    /// <param name="databaseOptions">Current database configuration.</param>
    public CloudSyncWindowViewModel(
        ILogger logger,
        ICloudSyncService cloudSyncService,
        DatabaseOptions databaseOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cloudSyncService = cloudSyncService ?? throw new ArgumentNullException(nameof(cloudSyncService));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));

        IsBackendConfigured = !string.IsNullOrWhiteSpace(databaseOptions.BackendApiUrl);
        CompanyName = string.Empty;
        ThrottleMs = DefaultThrottleMs;
        IsSyncing = false;
        ProgressValue = 0;
        ProgressText = string.Empty;
        StatusMessage = string.Empty;
        IsStatusError = false;
        TotalFunds = 0;
        SuccessCount = 0;
        FailCount = 0;

        SyncCommand = new DelegateCommand(ExecuteSync, CanExecuteSync, true);
        CloseCommand = new DelegateCommand(ExecuteClose);
        WindowClosingCommand = new DelegateCommand(ExecuteWindowClosing);

        _logger.Debug("CloudSyncWindowViewModel initialized, IsBackendConfigured={0}", IsBackendConfigured);
    }

    /// <summary>
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public CloudSyncWindowViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _cloudSyncService = null!;
        _databaseOptions = new DatabaseOptions();

        IsBackendConfigured = true;
        CompanyName = "Handelsbanken";
        ThrottleMs = DefaultThrottleMs;
        IsSyncing = false;
        ProgressValue = 0;
        ProgressText = string.Empty;
        StatusMessage = string.Empty;
        IsStatusError = false;
        TotalFunds = 0;
        SuccessCount = 0;
        FailCount = 0;

        SyncCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
        WindowClosingCommand = new DelegateCommand(() => { });
    }

    #region Properties

    /// <summary>
    /// Gets whether the Backend API URL is configured.
    /// </summary>
    public bool IsBackendConfigured { get; }

    /// <summary>
    /// Gets or sets the company name filter.
    /// </summary>
    public string CompanyName
    {
        get => GetProperty(() => CompanyName);
        set => SetProperty(() => CompanyName, value);
    }

    /// <summary>
    /// Gets or sets the throttle delay in milliseconds between per-fund API calls.
    /// </summary>
    public int ThrottleMs
    {
        get => GetProperty(() => ThrottleMs);
        set => SetProperty(() => ThrottleMs, value);
    }

    /// <summary>
    /// Gets or sets whether a sync operation is in progress.
    /// </summary>
    public bool IsSyncing
    {
        get => GetProperty(() => IsSyncing);
        set => SetProperty(() => IsSyncing, value);
    }

    /// <summary>
    /// Gets or sets the progress bar value (0–100).
    /// </summary>
    public int ProgressValue
    {
        get => GetProperty(() => ProgressValue);
        set => SetProperty(() => ProgressValue, value);
    }

    /// <summary>
    /// Gets or sets the progress description text.
    /// </summary>
    public string ProgressText
    {
        get => GetProperty(() => ProgressText);
        set => SetProperty(() => ProgressText, value);
    }

    /// <summary>
    /// Gets or sets the status message displayed after sync completes.
    /// </summary>
    public string StatusMessage
    {
        get => GetProperty(() => StatusMessage);
        set => SetProperty(() => StatusMessage, value);
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
    /// Gets or sets the total number of funds matched by the filter.
    /// </summary>
    public int TotalFunds
    {
        get => GetProperty(() => TotalFunds);
        set => SetProperty(() => TotalFunds, value);
    }

    /// <summary>
    /// Gets or sets the number of funds synced successfully.
    /// </summary>
    public int SuccessCount
    {
        get => GetProperty(() => SuccessCount);
        set => SetProperty(() => SuccessCount, value);
    }

    /// <summary>
    /// Gets or sets the number of funds that failed to sync.
    /// </summary>
    public int FailCount
    {
        get => GetProperty(() => FailCount);
        set => SetProperty(() => FailCount, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to execute the sync operation.
    /// </summary>
    public ICommand SyncCommand { get; }

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

    private bool CanExecuteSync() => !IsSyncing && IsBackendConfigured;

    private async void ExecuteSync()
    {
        var companyFilter = string.IsNullOrWhiteSpace(CompanyName) ? null : CompanyName.Trim();
        _logger.Info("Cloud sync started: company={0}, throttle={1}ms", companyFilter ?? "(all)", ThrottleMs);

        IsSyncing = true;
        StatusMessage = string.Empty;
        IsStatusError = false;
        ProgressValue = 0;
        ProgressText = "Querying funds...";
        TotalFunds = 0;
        SuccessCount = 0;
        FailCount = 0;

        _cts = new CancellationTokenSource();
        var sw = Stopwatch.StartNew();

        try
        {
            var progress = new Progress<CloudSyncProgress>(OnProgressReport);
            var result = await _cloudSyncService.SyncAsync(companyFilter, ThrottleMs, progress, _cts.Token);

            sw.Stop();

            if (result.WasCancelled)
            {
                StatusMessage = $"Sync cancelled after {result.HistoryRecordsSynced} history records";
                IsStatusError = false;
                _logger.Info("Cloud sync cancelled after {0}", sw.Elapsed);
            }
            else if (result.FailedFunds > 0)
            {
                StatusMessage = $"Synced with {result.FailedFunds} failures — {result.HistoryRecordsSynced} history records in {sw.Elapsed:mm\\:ss}";
                IsStatusError = true;
                _logger.Warn("Cloud sync completed with failures: {0}", result);
            }
            else
            {
                StatusMessage = $"Synced {result.TotalFunds} funds, {result.HistoryRecordsSynced} history records in {sw.Elapsed:mm\\:ss}";
                IsStatusError = false;
                _logger.Info("Cloud sync completed: {0} funds, {1} history records in {2}",
                    result.TotalFunds, result.HistoryRecordsSynced, sw.Elapsed);
            }

            ProgressText = string.Empty;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Sync cancelled";
            IsStatusError = false;
            _logger.Info("Cloud sync cancelled by user");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync failed: {ex.Message}";
            IsStatusError = true;
            _logger.Error(ex, "Cloud sync failed");
        }
        finally
        {
            IsSyncing = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    private void ExecuteClose()
    {
        _logger.Debug("Cloud sync window close requested");
        CurrentWindowService?.Close();
    }

    private void ExecuteWindowClosing()
    {
        _logger.Debug("Cloud sync window closing — cancelling any active sync");
        _cts?.Cancel();
    }

    #endregion

    #region Helpers

    private void OnProgressReport(CloudSyncProgress p)
    {
        TotalFunds = p.TotalFunds;
        SuccessCount = p.SuccessCount;
        FailCount = p.FailCount;
        ProgressText = p.Phase;
        ProgressValue = p.TotalFunds > 0
            ? (int)(100.0 * p.ProcessedFunds / p.TotalFunds)
            : 0;
    }

    #endregion
}
