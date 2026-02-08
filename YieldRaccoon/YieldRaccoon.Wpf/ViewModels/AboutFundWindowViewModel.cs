using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events.AboutFund;
using YieldRaccoon.Wpf.Configuration;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the AboutFund browser window with 3-column layout.
/// </summary>
/// <remarks>
/// Orchestrates child ViewModels for fund schedule (left), browser (middle), and control panel (right).
/// Subscribes to the <see cref="IAboutFundOrchestrator"/> for navigation and state updates.
/// </remarks>
public class AboutFundWindowViewModel : ViewModelBase, IDisposable
{
    private const string BlankPageUrl = "about:blank";

    private readonly ILogger _logger;
    private readonly YieldRaccoonOptions _options;
    private readonly IAboutFundOrchestrator _orchestrator;
    private readonly IScheduler _uiScheduler;
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when the window should close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Event raised when browser reload is requested.
    /// </summary>
    public event EventHandler? BrowserReloadRequested;

    /// <summary>
    /// Event raised when navigation is requested.
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    #region Child ViewModels

    /// <summary>
    /// Gets the fund schedule ViewModel (left panel).
    /// </summary>
    public AboutFundScheduleViewModel FundScheduleViewModel { get; }

    /// <summary>
    /// Gets the control panel ViewModel (right panel).
    /// </summary>
    public AboutFundControlPanelViewModel ControlPanelViewModel { get; }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title
    {
        get => GetProperty(() => Title);
        set => SetProperty(() => Title, value);
    }

    /// <summary>
    /// Gets or sets the browser URL.
    /// </summary>
    public string BrowserUrl
    {
        get => GetProperty(() => BrowserUrl);
        set => SetProperty(() => BrowserUrl, value);
    }

    /// <summary>
    /// Gets or sets whether the browser is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get => GetProperty(() => IsLoading);
        set => SetProperty(() => IsLoading, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to navigate to a URL.
    /// </summary>
    public ICommand NavigateCommand { get; }

    /// <summary>
    /// Gets the command to reload the browser.
    /// </summary>
    public ICommand ReloadCommand { get; }

    /// <summary>
    /// Gets the command to close the window.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Gets the command executed when the window is loaded.
    /// </summary>
    public ICommand LoadedCommand { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundWindowViewModel"/> class.
    /// </summary>
    public AboutFundWindowViewModel(
        ILogger logger,
        YieldRaccoonOptions options,
        IAboutFundOrchestrator orchestrator,
        AboutFundScheduleViewModel fundScheduleViewModel,
        AboutFundControlPanelViewModel controlPanelViewModel,
        IScheduler uiScheduler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _uiScheduler = uiScheduler ?? throw new ArgumentNullException(nameof(uiScheduler));

        FundScheduleViewModel = fundScheduleViewModel ?? throw new ArgumentNullException(nameof(fundScheduleViewModel));
        ControlPanelViewModel = controlPanelViewModel ?? throw new ArgumentNullException(nameof(controlPanelViewModel));

        Title = "AboutFund - Overview";
        BrowserUrl = BlankPageUrl;
        IsLoading = false;

        // Initialize AutoStartOverview from options
        ControlPanelViewModel.AutoStartOverview = _options.AutoStartOverview;

        // Initialize commands
        NavigateCommand = new DelegateCommand(ExecuteNavigate);
        ReloadCommand = new DelegateCommand(ExecuteReload);
        CloseCommand = new DelegateCommand(ExecuteClose);
        LoadedCommand = new DelegateCommand(ExecuteLoaded);

        // Wire control panel events to orchestrator
        ControlPanelViewModel.StartOverviewRequested += OnStartOverviewRequested;
        ControlPanelViewModel.StopOverviewRequested += OnStopOverviewRequested;
        ControlPanelViewModel.NextFundRequested += OnNextFundRequested;

        // Subscribe to orchestrator observables
        SetupOrchestratorSubscriptions();

        _logger.Debug("AboutFundWindowViewModel initialized");
    }

    /// <summary>
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public AboutFundWindowViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _options = new YieldRaccoonOptions { FundDetailsPageUrlTemplate = "https://example.com/" };
        _orchestrator = null!;
        _uiScheduler = null!;

        FundScheduleViewModel = new AboutFundScheduleViewModel(LogManager.GetCurrentClassLogger());
        ControlPanelViewModel = new AboutFundControlPanelViewModel(LogManager.GetCurrentClassLogger());

        Title = "AboutFund - Overview (Design)";
        BrowserUrl = BlankPageUrl;
        IsLoading = false;

        NavigateCommand = new DelegateCommand(() => { });
        ReloadCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
        LoadedCommand = new DelegateCommand(() => { });
    }

    #region Initialization

    /// <summary>
    /// Executes when the window is loaded.
    /// Loads the fund schedule and navigates to the first fund.
    /// If AutoStartOverview is enabled, starts the full browsing session.
    /// </summary>
    private async void ExecuteLoaded()
    {
        _logger.Info("Window loaded - initializing AboutFund");

        try
        {
            // Load fund schedule from database
            var schedule = await _orchestrator.LoadScheduleAsync();
            FundScheduleViewModel.LoadSchedule(schedule);

            _logger.Info("Fund schedule loaded: {0} funds", schedule.Count);

            if (schedule.Count == 0)
            {
                _logger.Warn("No funds in schedule - nothing to display");
                return;
            }

            if (ControlPanelViewModel.AutoStartOverview)
            {
                // Auto-start: begin full browsing session with auto-advance
                _logger.Info("Auto-start enabled - starting browsing session");
                _orchestrator.SetAutoAdvance(true);
                await _orchestrator.StartSessionAsync();
            }
            else
            {
                // Navigate to first fund without starting a session
                NavigateToFirstFund(schedule);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize AboutFund window");
        }
    }

    /// <summary>
    /// Navigates to the first fund in the schedule that has an OrderbookId.
    /// </summary>
    private void NavigateToFirstFund(IReadOnlyList<Application.Models.AboutFundScheduleItem> schedule)
    {
        var firstFund = schedule.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.OrderbookId));
        if (firstFund == null)
        {
            _logger.Warn("No funds with OrderbookId in schedule");
            return;
        }

        var url = _options.GetFundDetailsUrlByOrderbookId(firstFund.OrderbookId!);
        _logger.Info("Navigating to first fund: {0}", url);
        BrowserUrl = url;
        NavigationRequested?.Invoke(this, url);
    }

    #endregion

    #region Orchestrator Subscriptions

    private void SetupOrchestratorSubscriptions()
    {
        // Navigate to URL
        _disposables.Add(
            _orchestrator.NavigateToUrl
                .ObserveOn(_uiScheduler)
                .Subscribe(OnNavigateToUrl));

        // Session state changes
        _disposables.Add(
            _orchestrator.SessionState
                .ObserveOn(_uiScheduler)
                .Subscribe(OnSessionStateChanged));

        // Events
        _disposables.Add(
            _orchestrator.Events
                .ObserveOn(_uiScheduler)
                .Subscribe(OnEventReceived));

        // Countdown ticks
        _disposables.Add(
            _orchestrator.CountdownTick
                .ObserveOn(_uiScheduler)
                .Subscribe(OnCountdownTick));
    }

    private void OnNavigateToUrl(string url)
    {
        _logger.Debug("Orchestrator requests navigation to: {0}", url);
        BrowserUrl = url;
        NavigationRequested?.Invoke(this, url);
    }

    private void OnSessionStateChanged(Application.Models.AboutFundSessionState state)
    {
        ControlPanelViewModel.OnSessionStateChanged(state);

        if (state.IsActive) FundScheduleViewModel.MarkCurrentFund(state.CurrentIndex);
    }

    private void OnEventReceived(IAboutFundEvent aboutFundEvent)
    {
        ControlPanelViewModel.OnEventReceived(aboutFundEvent);

        // Mark fund as completed in schedule
        if (aboutFundEvent is AboutFundNavigationCompleted completed)
            FundScheduleViewModel.MarkCompleted(completed.Index);
    }

    private void OnCountdownTick(int secondsRemaining)
    {
        ControlPanelViewModel.OnCountdownTick(secondsRemaining);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Called when browser loading state changes.
    /// Called from <see cref="YieldRaccoon.Wpf.Behaviors.AboutFundWebView2Behavior"/>.
    /// </summary>
    public void OnBrowserLoadingChanged(bool isLoading)
    {
        IsLoading = isLoading;

        // Notify orchestrator when navigation completes
        if (!isLoading) _orchestrator.NotifyNavigationCompleted();
    }

    #endregion

    #region Command Implementations

    private void ExecuteNavigate()
    {
        if (!string.IsNullOrWhiteSpace(BrowserUrl))
        {
            _logger.Info("Navigating to: {0}", BrowserUrl);
            NavigationRequested?.Invoke(this, BrowserUrl);
        }
    }

    private void ExecuteReload()
    {
        _logger.Debug("Reload requested");
        BrowserReloadRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteClose()
    {
        _logger.Debug("Close requested");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Control Panel Event Handlers

    private async void OnStartOverviewRequested(object? sender, EventArgs e)
    {
        try
        {
            _orchestrator.SetAutoAdvance(ControlPanelViewModel.AutoStartOverview);
            await _orchestrator.StartSessionAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start overview session");
        }
    }

    private void OnStopOverviewRequested(object? sender, EventArgs e)
    {
        _orchestrator.CancelSession("User stopped");
    }

    private void OnNextFundRequested(object? sender, EventArgs e)
    {
        _orchestrator.AdvanceToNextFund();
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _logger.Debug("AboutFundWindowViewModel disposing");

            // Cancel active session on window close
            _orchestrator.CancelSession("Window closed");

            // Unsubscribe from control panel events
            ControlPanelViewModel.StartOverviewRequested -= OnStartOverviewRequested;
            ControlPanelViewModel.StopOverviewRequested -= OnStopOverviewRequested;
            ControlPanelViewModel.NextFundRequested -= OnNextFundRequested;

            _disposables.Dispose();
        }

        _disposed = true;
    }

    #endregion
}