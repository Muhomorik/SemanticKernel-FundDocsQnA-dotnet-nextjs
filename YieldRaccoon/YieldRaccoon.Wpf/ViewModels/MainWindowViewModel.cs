using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Media;
using DevExpress.Mvvm;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Mappers;
using YieldRaccoon.Wpf.Models;
using YieldRaccoon.Wpf.Services;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// Implements MVVM pattern using DevExpress MVVM framework.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel follows DDD layer separation principles:
/// <list type="bullet">
///   <item>Manages pure visual components (streaming toggle, visibility states)</item>
///   <item>Subscribes to observable streams from orchestrator for state updates</item>
///   <item>Delegates all business logic to <see cref="ICrawlSessionOrchestrator"/></item>
///   <item>Does NOT directly manipulate the event store</item>
/// </list>
/// </para>
/// </remarks>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger _logger;
    private readonly IScheduler _uiScheduler;
    private readonly YieldRaccoonOptions _options;
    private readonly ICrawlSessionOrchestrator _orchestrator;
    private readonly ISettingsDialogService _settingsDialogService;
    private readonly IAboutFundWindowService _aboutFundWindowService;
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    #region UI State Properties

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title
    {
        get => GetProperty(() => Title);
        set => SetProperty(() => Title, value);
    }

    /// <summary>
    /// Gets or sets the status message displayed in the UI.
    /// </summary>
    public string StatusMessage
    {
        get => GetProperty(() => StatusMessage);
        set => SetProperty(() => StatusMessage, value);
    }

    /// <summary>
    /// Gets or sets the URL for the browser to navigate to.
    /// </summary>
    public string BrowserUrl
    {
        get => GetProperty(() => BrowserUrl);
        set => SetProperty(() => BrowserUrl, value);
    }

    /// <summary>
    /// Gets or sets whether the browser is loading.
    /// </summary>
    public bool IsLoading
    {
        get => GetProperty(() => IsLoading);
        set => SetProperty(() => IsLoading, value);
    }

    /// <summary>
    /// Gets or sets whether WebView2 is initialized and ready for commands.
    /// </summary>
    public bool IsWebView2Ready
    {
        get => GetProperty(() => IsWebView2Ready);
        set => SetProperty(() => IsWebView2Ready, value);
    }

    #endregion

    #region Fund Data Properties

    /// <summary>
    /// Gets the observable collection of intercepted funds.
    /// </summary>
    public ObservableCollection<InterceptedFundViewModel> Funds { get; } = new();

    /// <summary>
    /// Gets or sets the count of intercepted funds.
    /// </summary>
    public int FundCount
    {
        get => GetProperty(() => FundCount);
        set => SetProperty(() => FundCount, value);
    }

    /// <summary>
    /// Gets or sets the total count of funds available (for pagination).
    /// </summary>
    public int TotalFundCount
    {
        get => GetProperty(() => TotalFundCount);
        set => SetProperty(() => TotalFundCount, value);
    }

    /// <summary>
    /// Gets or sets whether pagination is currently in progress.
    /// </summary>
    public bool IsPaginationInProgress
    {
        get => GetProperty(() => IsPaginationInProgress);
        set => SetProperty(() => IsPaginationInProgress, value);
    }

    #endregion

    #region Session State Properties (Bound from Orchestrator)

    /// <summary>
    /// Gets or sets whether a crawl session is currently active.
    /// </summary>
    public bool IsSessionActive
    {
        get => GetProperty(() => IsSessionActive);
        set => SetProperty(() => IsSessionActive, value);
    }

    /// <summary>
    /// Gets or sets whether auto-start is enabled for sessions.
    /// </summary>
    public bool IsAutoStartEnabled
    {
        get => GetProperty(() => IsAutoStartEnabled);
        set => SetProperty(() => IsAutoStartEnabled, value);
    }

    /// <summary>
    /// Gets or sets whether a delay timer is in progress before next batch.
    /// </summary>
    public bool IsDelayInProgress
    {
        get => GetProperty(() => IsDelayInProgress);
        set => SetProperty(() => IsDelayInProgress, value);
    }

    /// <summary>
    /// Gets or sets the current batch number being processed.
    /// </summary>
    public int CurrentBatchNumber
    {
        get => GetProperty(() => CurrentBatchNumber);
        set => SetProperty(() => CurrentBatchNumber, value);
    }

    /// <summary>
    /// Gets or sets the estimated total number of batches.
    /// </summary>
    public int EstimatedBatchCount
    {
        get => GetProperty(() => EstimatedBatchCount);
        set => SetProperty(() => EstimatedBatchCount, value);
    }

    /// <summary>
    /// Gets or sets the estimated time remaining for the session.
    /// </summary>
    public TimeSpan EstimatedTimeRemaining
    {
        get => GetProperty(() => EstimatedTimeRemaining);
        set => SetProperty(() => EstimatedTimeRemaining, value);
    }

    /// <summary>
    /// Gets or sets the session status message.
    /// </summary>
    public string SessionStatusMessage
    {
        get => GetProperty(() => SessionStatusMessage);
        set => SetProperty(() => SessionStatusMessage, value);
    }

    /// <summary>
    /// Gets or sets the countdown seconds remaining before next action.
    /// </summary>
    public int DelayCountdown
    {
        get => GetProperty(() => DelayCountdown);
        set => SetProperty(() => DelayCountdown, value);
    }

    /// <summary>
    /// Gets the observable collection of scheduled batches for UI display.
    /// </summary>
    public ObservableCollection<ScheduledBatchItemViewModel> ScheduledBatches { get; } = new();

    #endregion

    #region Streaming Mode Properties

    /// <summary>
    /// Gets or sets whether streaming mode is enabled (applies privacy filter to browser content).
    /// </summary>
    public bool IsStreamingMode
    {
        get => GetProperty(() => IsStreamingMode);
        set
        {
            SetProperty(() => IsStreamingMode, value, () => { StreamingModeChanged?.Invoke(this, EventArgs.Empty); });
        }
    }

    /// <summary>
    /// Gets or sets the captured browser screenshot with privacy filter applied.
    /// </summary>
    public ImageSource? StreamingScreenshot
    {
        get => GetProperty(() => StreamingScreenshot);
        set => SetProperty(() => StreamingScreenshot, value);
    }

    /// <summary>
    /// Event raised when streaming mode is toggled and a screenshot needs to be captured.
    /// </summary>
    public event EventHandler? StreamingModeChanged;

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to refresh the application state.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command to reload the browser page.
    /// </summary>
    public ICommand ReloadBrowserCommand { get; }

    /// <summary>
    /// Gets the command to load the next batch of funds (single click of "Visa fler").
    /// </summary>
    public ICommand LoadNextBatchCommand { get; }

    /// <summary>
    /// Gets the command to start an automated crawl session.
    /// </summary>
    public ICommand StartSessionCommand { get; }

    /// <summary>
    /// Gets the command to stop the current crawl session.
    /// </summary>
    public ICommand StopSessionCommand { get; }

    /// <summary>
    /// Gets the command executed when the window is loaded.
    /// </summary>
    public ICommand LoadedCommand { get; }

    /// <summary>
    /// Gets the command to open the settings window.
    /// </summary>
    public ICommand OpenSettingsCommand { get; }

    /// <summary>
    /// Gets the command to open the AboutFund browser window.
    /// </summary>
    public ICommand OpenAboutFundCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when browser reload is requested.
    /// </summary>
    public event EventHandler? BrowserReloadRequested;

    /// <summary>
    /// Event raised when the ViewModel detects that more funds should be loaded.
    /// The View should click the "Visa fler" button when this event fires.
    /// </summary>
    public event EventHandler? RequestLoadMoreFunds;

    /// <summary>
    /// Event raised when the browser should scroll to the bottom of the page.
    /// The View should execute smooth scroll JavaScript when this event fires.
    /// </summary>
    public event EventHandler? BrowserScrollToEndRequested;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// Runtime constructor - dependencies injected via DI container.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="uiScheduler">Scheduler for marshalling operations to UI thread.</param>
    /// <param name="options">Configuration options containing URLs for fund pages.</param>
    /// <param name="orchestrator">Orchestrator for session lifecycle and batch workflow.</param>
    /// <param name="settingsDialogService">Service for showing the settings dialog.</param>
    /// <param name="aboutFundWindowService"></param>
    public MainWindowViewModel(
        ILogger logger,
        IScheduler uiScheduler,
        YieldRaccoonOptions options,
        ICrawlSessionOrchestrator orchestrator,
        ISettingsDialogService settingsDialogService,
        IAboutFundWindowService aboutFundWindowService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uiScheduler = uiScheduler ?? throw new ArgumentNullException(nameof(uiScheduler));
        _settingsDialogService =
            settingsDialogService ?? throw new ArgumentNullException(nameof(settingsDialogService));
        _aboutFundWindowService =
            aboutFundWindowService ?? throw new ArgumentNullException(nameof(aboutFundWindowService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

        _logger.Info("MainWindowViewModel constructor called");

        // Initialize UI state properties
        Title = "Yield Raccoon - we walk in the dark";
        StatusMessage = "Ready";
        BrowserUrl = _options.FundListPageUrlOverviewTab;
        IsLoading = false;
        IsWebView2Ready = false;
        FundCount = 0;
        TotalFundCount = 0;
        IsPaginationInProgress = false;

        // Initialize session state properties
        IsSessionActive = false;
        IsAutoStartEnabled = false;
        IsDelayInProgress = false;
        CurrentBatchNumber = 0;
        EstimatedBatchCount = 0;
        EstimatedTimeRemaining = TimeSpan.Zero;
        SessionStatusMessage = string.Empty;
        DelayCountdown = 0;
        IsStreamingMode = false;

        // Initialize commands with CommandManager integration enabled
        RefreshCommand = new DelegateCommand(ExecuteRefresh, CanExecuteRefresh, true);
        ReloadBrowserCommand = new DelegateCommand(ExecuteReloadBrowser, CanExecuteReloadBrowser, true);
        LoadNextBatchCommand = new DelegateCommand(ExecuteLoadNextBatch, CanExecuteLoadNextBatch, true);
        StartSessionCommand = new DelegateCommand(ExecuteStartSession, CanExecuteStartSession, true);
        StopSessionCommand = new DelegateCommand(ExecuteStopSession, CanExecuteStopSession, true);
        LoadedCommand = new DelegateCommand(ExecuteLoaded);
        OpenSettingsCommand = new DelegateCommand(ExecuteOpenSettings);
        OpenAboutFundCommand = new DelegateCommand(ExecuteOpenAboutFund);

        // Set up subscriptions to orchestrator streams
        SetupOrchestratorSubscriptions();

        _logger.Debug("MainWindowViewModel initialized with URL: {0}", BrowserUrl);
    }

    /// <summary>
    /// Design-time constructor for XAML designer support.
    /// </summary>
    public MainWindowViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _uiScheduler = DispatcherScheduler.Current;
        _options = new YieldRaccoonOptions { FundListPageUrlOverviewTab = "https://example.com" };
        _orchestrator = null!; // Design-time only
        _settingsDialogService = null!; // Design-time only
        _aboutFundWindowService = null!; // Design-time only

        Title = "Yield Raccoon - we walk in the dark (Design Time)";
        StatusMessage = "Ready";
        BrowserUrl = "https://example.com";
        IsLoading = false;
        IsWebView2Ready = true;
        FundCount = 0;
        TotalFundCount = 0;
        IsPaginationInProgress = false;

        // Design-time session properties
        IsSessionActive = false;
        IsAutoStartEnabled = false;
        IsDelayInProgress = false;
        CurrentBatchNumber = 0;
        EstimatedBatchCount = 0;
        EstimatedTimeRemaining = TimeSpan.Zero;
        SessionStatusMessage = string.Empty;
        DelayCountdown = 0;
        IsStreamingMode = false;

        RefreshCommand = new DelegateCommand(() => { });
        ReloadBrowserCommand = new DelegateCommand(() => { });
        LoadNextBatchCommand = new DelegateCommand(() => { });
        StartSessionCommand = new DelegateCommand(() => { });
        StopSessionCommand = new DelegateCommand(() => { });
        LoadedCommand = new DelegateCommand(() => { });
        OpenSettingsCommand = new DelegateCommand(() => { });
        OpenAboutFundCommand = new DelegateCommand(() => { });
    }

    #region Orchestrator Subscriptions

    /// <summary>
    /// Sets up subscriptions to orchestrator observable streams.
    /// </summary>
    private void SetupOrchestratorSubscriptions()
    {
        // Session state updates
        _orchestrator.SessionState
            .ObserveOn(_uiScheduler)
            .Subscribe(OnSessionStateChanged)
            .DisposeWith(_disposables);

        // Scheduled batches list
        _orchestrator.ScheduledBatches
            .ObserveOn(_uiScheduler)
            .Subscribe(OnScheduledBatchesChanged)
            .DisposeWith(_disposables);

        // Countdown ticks
        _orchestrator.CountdownTick
            .ObserveOn(_uiScheduler)
            .Subscribe(OnCountdownTick)
            .DisposeWith(_disposables);

        // Load batch requests - delegate to view via event
        _orchestrator.LoadBatchRequested
            .ObserveOn(_uiScheduler)
            .Subscribe(_ => RequestLoadMoreFunds?.Invoke(this, EventArgs.Empty))
            .DisposeWith(_disposables);

        // Session completed
        _orchestrator.SessionCompleted
            .ObserveOn(_uiScheduler)
            .Subscribe(OnSessionCompleted)
            .DisposeWith(_disposables);

        _logger.Debug("Orchestrator subscriptions configured");
    }

    /// <summary>
    /// Handles session state changes from the orchestrator.
    /// </summary>
    private void OnSessionStateChanged(CrawlSessionState state)
    {
        _logger.Trace("Session state changed: Active={0}, Batch={1}/{2}",
            state.IsActive, state.CurrentBatchNumber, state.EstimatedBatchCount);

        IsSessionActive = state.IsActive;
        CurrentBatchNumber = state.CurrentBatchNumber;
        EstimatedBatchCount = state.EstimatedBatchCount;
        EstimatedTimeRemaining = state.EstimatedTimeRemaining;
        IsDelayInProgress = state.IsDelayInProgress;
        SessionStatusMessage = state.StatusMessage;
        DelayCountdown = state.DelayCountdown;

        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Handles scheduled batches list changes from the orchestrator.
    /// </summary>
    private void OnScheduledBatchesChanged(IReadOnlyList<ScheduledBatchItem> batches)
    {
        _logger.Trace("Scheduled batches updated: {0} items", batches.Count);

        // Update existing items or add new ones (preserves ViewModels for proper INPC)
        foreach (var batch in batches)
        {
            var existing = ScheduledBatches.FirstOrDefault(vm => vm.BatchNumber.Value == batch.BatchNumber.Value);

            if (existing != null)
                // Update existing ViewModel - this triggers INPC properly
                existing.UpdateFrom(batch);
            else
                // Add new ViewModel
                ScheduledBatches.Add(ScheduledBatchItemViewModel.FromModel(batch));
        }

        // Remove items that no longer exist in the source
        var batchNumbers = batches.Select(b => b.BatchNumber.Value).ToHashSet();
        var toRemove = ScheduledBatches
            .Where(vm => !batchNumbers.Contains(vm.BatchNumber.Value))
            .ToList();

        foreach (var item in toRemove) ScheduledBatches.Remove(item);
    }

    /// <summary>
    /// Handles countdown tick events from the orchestrator.
    /// </summary>
    private void OnCountdownTick(CountdownTick tick)
    {
        DelayCountdown = tick.SecondsRemaining;
        SessionStatusMessage = "Next batch in";
    }

    /// <summary>
    /// Handles session completed events from the orchestrator.
    /// </summary>
    private void OnSessionCompleted(SessionCompletedInfo info)
    {
        _logger.Info("Session completed: {0} funds in {1}",
            info.TotalFundsLoaded, info.Duration);

        SessionStatusMessage = $"Complete! Loaded {info.TotalFundsLoaded} funds in {info.Duration:mm\\:ss}";
        StatusMessage = $"Session complete at {DateTime.Now:HH:mm:ss}";

        CommandManager.InvalidateRequerySuggested();
    }

    #endregion

    #region Command Handlers

    /// <summary>
    /// Executes when the window is loaded.
    /// Sets initial state for UI components.
    /// </summary>
    private void ExecuteLoaded()
    {
        _logger.Info("Window loaded - setting initial state");
        IsAutoStartEnabled = false;
    }

    /// <summary>
    /// Executes the open settings command.
    /// Shows the settings dialog via the dialog service.
    /// </summary>
    private void ExecuteOpenSettings()
    {
        _logger.Debug("Open settings command executed");

        var saved = _settingsDialogService.ShowSettingsDialog();

        if (saved)
        {
            _logger.Info("Settings saved - restart required for changes to take effect");
            StatusMessage = "Settings saved. Restart the application to apply changes.";
        }
    }

    /// <summary>
    /// Executes the open AboutFund command.
    /// Shows the AboutFund browser window via the window service.
    /// </summary>
    private void ExecuteOpenAboutFund()
    {
        _logger.Debug("Open AboutFund window command executed");
        _aboutFundWindowService.ShowAboutFundWindow();
    }

    /// <summary>
    /// Determines whether the refresh command can be executed.
    /// </summary>
    private bool CanExecuteRefresh()
    {
        var canExecute = IsWebView2Ready;
        return canExecute;
    }

    /// <summary>
    /// Executes the refresh command.
    /// </summary>
    private void ExecuteRefresh()
    {
        _logger.Debug("Refresh command executed");
        ResetFunds();
        BrowserReloadRequested?.Invoke(this, EventArgs.Empty);
        StatusMessage = $"Browser refreshed at {DateTime.Now:HH:mm:ss}";
    }

    /// <summary>
    /// Determines whether the reload browser command can be executed.
    /// </summary>
    private bool CanExecuteReloadBrowser()
    {
        var canExecute = !string.IsNullOrWhiteSpace(BrowserUrl) && IsWebView2Ready;
        return canExecute;
    }

    /// <summary>
    /// Executes the reload browser command.
    /// </summary>
    private void ExecuteReloadBrowser()
    {
        _logger.Info("Browser reload requested");
        ResetFunds();
        BrowserReloadRequested?.Invoke(this, EventArgs.Empty);
        StatusMessage = $"Browser reloaded at {DateTime.Now:HH:mm:ss}";
    }

    /// <summary>
    /// Determines whether the load next batch command can be executed.
    /// </summary>
    private bool CanExecuteLoadNextBatch()
    {
        var canExecute = IsWebView2Ready && FundCount > 0 && !IsPaginationInProgress && TotalFundCount > FundCount;
        return canExecute;
    }

    /// <summary>
    /// Executes the load next batch command.
    /// </summary>
    private void ExecuteLoadNextBatch()
    {
        _logger.Info("Load next batch command executed");
        StatusMessage = $"Loading next batch... ({FundCount}/{TotalFundCount})";
        RequestLoadMoreFunds?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Determines whether the start session command can be executed.
    /// </summary>
    private bool CanExecuteStartSession()
    {
        var canExecute = IsWebView2Ready && FundCount > 0 && !IsSessionActive && TotalFundCount > FundCount;
        return canExecute;
    }

    /// <summary>
    /// Executes the start session command.
    /// Delegates to orchestrator for session lifecycle management.
    /// </summary>
    private void ExecuteStartSession()
    {
        _logger.Info("Starting crawl session via orchestrator");
        _orchestrator.StartSession();
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Determines whether the stop session command can be executed.
    /// </summary>
    private bool CanExecuteStopSession()
    {
        return IsSessionActive;
    }

    /// <summary>
    /// Executes the stop session command.
    /// Delegates to orchestrator for session cancellation.
    /// </summary>
    private void ExecuteStopSession()
    {
        _logger.Info("Stopping crawl session via orchestrator");
        _orchestrator.CancelSession("User cancelled");
        CommandManager.InvalidateRequerySuggested();
    }

    #endregion

    #region Browser Event Handlers

    /// <summary>
    /// Handles browser loading state change.
    /// </summary>
    /// <param name="isLoading">Whether the browser is currently loading.</param>
    public void OnBrowserLoadingStateChanged(bool isLoading)
    {
        _logger.Debug("Browser loading state changed: {0}", isLoading);
        IsLoading = isLoading;
        StatusMessage = isLoading ? "Loading..." : "Ready";
    }

    /// <summary>
    /// Notifies the ViewModel that WebView2 initialization is complete.
    /// </summary>
    public void OnWebView2Initialized()
    {
        _logger.Info("WebView2 initialized - enabling browser commands");

        _uiScheduler.Schedule(() =>
        {
            IsWebView2Ready = true;
            StatusMessage = "Browser ready";
            CommandManager.InvalidateRequerySuggested();
            _logger.Debug("IsWebView2Ready set to: {0}", IsWebView2Ready);
        });
    }

    /// <summary>
    /// Handles intercepted fund data from network responses.
    /// Updates UI state and notifies orchestrator.
    /// </summary>
    /// <param name="fundData">The intercepted fund data.</param>
    public void OnFundDataReceived(InterceptedFundList? fundData)
    {
        if (fundData?.Funds == null || fundData.Funds.Count == 0)
        {
            _logger.Warn("Received empty or null fund data");
            return;
        }

        _logger.Info("Processing {0} intercepted funds", fundData.Funds.Count);

        // Update existing or add new funds (preserves ViewModels for proper INPC)
        var addedCount = 0;
        var updatedCount = 0;

        foreach (var fund in fundData.Funds)
        {
            if (string.IsNullOrEmpty(fund.Isin))
            {
                _logger.Trace("Skipping fund without ISIN: {0}", fund.Name);
                continue;
            }

            var existing = Funds.FirstOrDefault(vm => vm.Isin == fund.Isin);

            if (existing != null)
            {
                // Update existing ViewModel - triggers INPC
                existing.UpdateFrom(fund);
                updatedCount++;
                _logger.Trace("Updated fund: {0} ({1})", fund.Name, fund.Isin);
            }
            else
            {
                // Add new ViewModel
                Funds.Add(InterceptedFundViewModel.FromModel(fund));
                addedCount++;
                _logger.Debug("Added fund: {0} ({1})", fund.Name, fund.Isin);
            }
        }

        // Update UI counts
        FundCount = Funds.Count;
        if (fundData.TotalCount.HasValue) TotalFundCount = fundData.TotalCount.Value;

        _logger.Info("Processed funds: {0} added, {1} updated (total: {2}/{3})",
            addedCount, updatedCount, FundCount, TotalFundCount);

        // Trigger smooth scroll to bottom of browser when new data arrives
        BrowserScrollToEndRequested?.Invoke(this, EventArgs.Empty);

        // Convert to DTOs for persistence and orchestrator notification
        var fundDtos = fundData.Funds.ToFundDataDtos();

        // Always persist funds to database (regardless of session state)
        if (fundDtos.Count > 0)
        {
            if (IsSessionActive)
                // Session active: orchestrator handles persistence + event publishing (fire-and-forget)
                _ = _orchestrator.NotifyBatchLoadedAsync(fundDtos, FundCount, fundData.HasMore);
            else
                // No session: just persist without session events (fire-and-forget)
                _ = _orchestrator.IngestFundsAsync(fundDtos).ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                        _logger.Info("Persisted {0} funds to database (no active session)", t.Result);
                });
        }

        // Handle UI status updates and pagination (only when no active session)
        if (!IsSessionActive)
        {
            if (IsPaginationInProgress)
            {
                // Legacy pagination mode (without session)
                StatusMessage = fundData.HasMore
                    ? $"Loaded {FundCount} of {TotalFundCount} funds (loading more...)"
                    : $"Loaded {FundCount} funds (complete) at {DateTime.Now:HH:mm:ss}";

                if (fundData.HasMore)
                {
                    _logger.Info("Pagination in progress - requesting next batch ({0}/{1})", FundCount, TotalFundCount);
                    RequestLoadMoreFunds?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    _logger.Info("All funds loaded");
                    IsPaginationInProgress = false;
                }
            }
            else
            {
                // Not in any automatic mode (manual load or initial page load)
                StatusMessage = fundData.HasMore
                    ? $"Loaded {FundCount} of {TotalFundCount} funds"
                    : $"Loaded {FundCount} funds (complete) at {DateTime.Now:HH:mm:ss}";
                _logger.Debug("More funds available ({0}/{1}) - waiting for user action", FundCount, TotalFundCount);
            }
        }
        // When session is active, status updates come from the orchestrator's SessionState observable

        CommandManager.InvalidateRequerySuggested();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Resets the fund collection for a fresh load.
    /// </summary>
    public void ResetFunds()
    {
        _logger.Info("Resetting funds collection");
        Funds.Clear();
        FundCount = 0;
        TotalFundCount = 0;
        IsPaginationInProgress = false;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases all resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _logger.Debug("MainWindowViewModel disposing");
            _disposables.Dispose();
        }

        _disposed = true;
    }

    #endregion
}