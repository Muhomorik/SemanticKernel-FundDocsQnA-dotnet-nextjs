using System.Collections.ObjectModel;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.Events.AboutFund;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the right panel showing session controls, options, and event log.
/// </summary>
public class AboutFundControlPanelViewModel : ViewModelBase
{
    private readonly ILogger _logger;

    #region Properties

    /// <summary>
    /// Gets or sets whether auto-start overview is enabled.
    /// </summary>
    public bool AutoStartOverview
    {
        get => GetProperty(() => AutoStartOverview);
        set => SetProperty(() => AutoStartOverview, value);
    }

    /// <summary>
    /// Gets or sets whether a session is currently active.
    /// </summary>
    public bool IsSessionActive
    {
        get => GetProperty(() => IsSessionActive);
        set => SetProperty(() => IsSessionActive, value);
    }

    /// <summary>
    /// Gets or sets the current fund index.
    /// </summary>
    public int CurrentIndex
    {
        get => GetProperty(() => CurrentIndex);
        set => SetProperty(() => CurrentIndex, value);
    }

    /// <summary>
    /// Gets or sets the total number of funds.
    /// </summary>
    public int TotalFunds
    {
        get => GetProperty(() => TotalFunds);
        set => SetProperty(() => TotalFunds, value);
    }

    /// <summary>
    /// Gets or sets the current status message.
    /// </summary>
    public string StatusMessage
    {
        get => GetProperty(() => StatusMessage);
        set => SetProperty(() => StatusMessage, value);
    }

    /// <summary>
    /// Gets or sets whether a delay countdown is in progress before the next fund.
    /// </summary>
    public bool IsDelayInProgress
    {
        get => GetProperty(() => IsDelayInProgress);
        set => SetProperty(() => IsDelayInProgress, value);
    }

    /// <summary>
    /// Gets or sets the countdown seconds remaining.
    /// </summary>
    public int DelayCountdown
    {
        get => GetProperty(() => DelayCountdown);
        set => SetProperty(() => DelayCountdown, value);
    }

    /// <summary>
    /// Gets or sets the estimated time remaining for the session.
    /// </summary>
    public TimeSpan EstimatedTimeRemaining
    {
        get => GetProperty(() => EstimatedTimeRemaining);
        set => SetProperty(() => EstimatedTimeRemaining, value);
    }

    #endregion

    #region Collections

    /// <summary>
    /// Gets the event log for the current session.
    /// </summary>
    public ObservableCollection<AboutFundEventViewModel> Events { get; } = new();

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to start a browsing session.
    /// </summary>
    public ICommand StartOverviewCommand { get; }

    /// <summary>
    /// Gets the command to stop the current session.
    /// </summary>
    public ICommand StopOverviewCommand { get; }

    /// <summary>
    /// Gets the command to advance to the next fund.
    /// </summary>
    public ICommand NextFundCommand { get; }

    #endregion

    /// <summary>
    /// Event raised when the user requests starting a session.
    /// </summary>
    public event EventHandler? StartOverviewRequested;

    /// <summary>
    /// Event raised when the user requests stopping a session.
    /// </summary>
    public event EventHandler? StopOverviewRequested;

    /// <summary>
    /// Event raised when the user requests advancing to the next fund.
    /// </summary>
    public event EventHandler? NextFundRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundControlPanelViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AboutFundControlPanelViewModel(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        StatusMessage = string.Empty;
        AutoStartOverview = false;

        StartOverviewCommand = new DelegateCommand(
            () => StartOverviewRequested?.Invoke(this, EventArgs.Empty),
            () => !IsSessionActive);
        StopOverviewCommand = new DelegateCommand(
            () => StopOverviewRequested?.Invoke(this, EventArgs.Empty),
            () => IsSessionActive);
        NextFundCommand = new DelegateCommand(
            () => NextFundRequested?.Invoke(this, EventArgs.Empty),
            () => IsSessionActive);

        _logger.Debug("AboutFundControlPanelViewModel initialized");
    }

    /// <summary>
    /// Called when an about-fund event is received.
    /// </summary>
    /// <param name="aboutFundEvent">The event to display.</param>
    public void OnEventReceived(IAboutFundEvent aboutFundEvent)
    {
        var eventVm = AboutFundEventViewModel.FromEvent(aboutFundEvent);
        Events.Insert(0, eventVm);
    }

    /// <summary>
    /// Called when session state changes.
    /// </summary>
    /// <param name="state">The new session state.</param>
    public void OnSessionStateChanged(AboutFundSessionState state)
    {
        IsSessionActive = state.IsActive;
        CurrentIndex = state.CurrentIndex;
        TotalFunds = state.TotalFunds;
        StatusMessage = state.StatusMessage;
        IsDelayInProgress = state.IsDelayInProgress;
        DelayCountdown = state.DelayCountdown;
        EstimatedTimeRemaining = state.EstimatedTimeRemaining;

        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Called when a countdown tick is received from the orchestrator.
    /// </summary>
    /// <param name="secondsRemaining">Seconds remaining until next fund.</param>
    public void OnCountdownTick(int secondsRemaining)
    {
        DelayCountdown = secondsRemaining;
        IsDelayInProgress = true;
    }
}
