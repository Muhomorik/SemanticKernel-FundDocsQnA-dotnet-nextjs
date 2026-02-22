using System.Collections.ObjectModel;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the right panel showing session controls, options, and live collection progress.
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

    /// <summary>
    /// Gets or sets the display name of the fund currently being collected.
    /// </summary>
    public string? CurrentFundName
    {
        get => GetProperty(() => CurrentFundName);
        set => SetProperty(() => CurrentFundName, value);
    }

    /// <summary>
    /// Gets or sets the ISIN of the fund currently being collected.
    /// </summary>
    public string? CurrentIsin
    {
        get => GetProperty(() => CurrentIsin);
        set => SetProperty(() => CurrentIsin, value);
    }

    /// <summary>
    /// Gets or sets the OrderBookId of the fund currently being collected.
    /// </summary>
    public string? CurrentOrderBookId
    {
        get => GetProperty(() => CurrentOrderBookId);
        set => SetProperty(() => CurrentOrderBookId, value);
    }

    /// <summary>
    /// Gets or sets whether collection is actively in progress (not idle, not delay).
    /// </summary>
    public bool IsCollecting
    {
        get => GetProperty(() => IsCollecting);
        set => SetProperty(() => IsCollecting, value);
    }

    /// <summary>
    /// Gets or sets the number of completed interaction steps.
    /// </summary>
    public int CompletedStepsCount
    {
        get => GetProperty(() => CompletedStepsCount);
        set => SetProperty(() => CompletedStepsCount, value);
    }

    /// <summary>
    /// Gets or sets the total number of interaction steps.
    /// </summary>
    public int TotalStepsCount
    {
        get => GetProperty(() => TotalStepsCount);
        set => SetProperty(() => TotalStepsCount, value);
    }

    /// <summary>
    /// Gets or sets the number of resolved data slots.
    /// </summary>
    public int ResolvedSlotsCount
    {
        get => GetProperty(() => ResolvedSlotsCount);
        set => SetProperty(() => ResolvedSlotsCount, value);
    }

    /// <summary>
    /// Gets or sets the total number of data slots (always 7).
    /// </summary>
    public int TotalSlotsCount
    {
        get => GetProperty(() => TotalSlotsCount);
        set => SetProperty(() => TotalSlotsCount, value);
    }

    /// <summary>
    /// Gets or sets the elapsed time for the current collection.
    /// </summary>
    public TimeSpan CollectionElapsed
    {
        get => GetProperty(() => CollectionElapsed);
        set => SetProperty(() => CollectionElapsed, value);
    }

    /// <summary>
    /// Gets or sets the remaining time for the current collection.
    /// </summary>
    public TimeSpan CollectionRemaining
    {
        get => GetProperty(() => CollectionRemaining);
        set => SetProperty(() => CollectionRemaining, value);
    }

    #endregion

    #region Collections

    /// <summary>
    /// Gets the interaction steps for the current collection.
    /// </summary>
    public ObservableCollection<AboutFundCollectionStepViewModel> CollectionSteps { get; } = new();

    /// <summary>
    /// Gets the data fetch slot chips for the current collection.
    /// </summary>
    public ObservableCollection<AboutFundDataSlotViewModel> DataSlots { get; } = new();

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
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public AboutFundControlPanelViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();

        StatusMessage = string.Empty;
        AutoStartOverview = false;

        StartOverviewCommand = new DelegateCommand(() => { });
        StopOverviewCommand = new DelegateCommand(() => { });
        NextFundCommand = new DelegateCommand(() => { });
    }

    /// <inheritdoc />
    protected override void OnInitializeInDesignMode()
    {
        // Simulate an active session mid-collection
        IsSessionActive = true;
        CurrentIndex = 3;
        TotalFunds = 20;
        StatusMessage = "Visiting fund 3 of 20";
        EstimatedTimeRemaining = TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(34);

        CurrentFundName = "Länsförsäkringar Global Index";
        CurrentIsin = "SE0000740698";
        CurrentOrderBookId = "325410";
        IsCollecting = true;

        // Steps: first 5 completed, 6th is current, rest pending
        CompletedStepsCount = 5;
        TotalStepsCount = 8;
        CollectionSteps.Add(new AboutFundCollectionStepViewModel { DisplayName = "Activate SEK view", Status = AboutFundCollectionStepStatus.Completed, DelayText = "0.0s" });
        CollectionSteps.Add(new AboutFundCollectionStepViewModel { DisplayName = "Select 1 month", Status = AboutFundCollectionStepStatus.Completed, DelayText = "1.2s" });
        CollectionSteps.Add(new AboutFundCollectionStepViewModel { DisplayName = "Select 3 months", Status = AboutFundCollectionStepStatus.Completed, DelayText = "2.4s" });
        CollectionSteps.Add(new AboutFundCollectionStepViewModel { DisplayName = "Select year to date", Status = AboutFundCollectionStepStatus.Completed, DelayText = "3.6s" });
        CollectionSteps.Add(new AboutFundCollectionStepViewModel { DisplayName = "Select 1 year", Status = AboutFundCollectionStepStatus.Completed, DelayText = "4.8s" });
        CollectionSteps.Add(new AboutFundCollectionStepViewModel { DisplayName = "Select 3 years", Status = AboutFundCollectionStepStatus.Pending, DelayText = "6.0s", IsCurrent = true });
        CollectionSteps.Add(new AboutFundCollectionStepViewModel { DisplayName = "Select 5 years", Status = AboutFundCollectionStepStatus.Pending, DelayText = "7.2s" });
        CollectionSteps.Add(new AboutFundCollectionStepViewModel { DisplayName = "Select max", Status = AboutFundCollectionStepStatus.Pending, DelayText = "8.4s" });

        // Slots: first 4 succeeded, rest pending
        ResolvedSlotsCount = 4;
        TotalSlotsCount = 7;
        DataSlots.Add(new AboutFundDataSlotViewModel { Label = "1M", Status = AboutFundFetchStatus.Succeeded });
        DataSlots.Add(new AboutFundDataSlotViewModel { Label = "3M", Status = AboutFundFetchStatus.Succeeded });
        DataSlots.Add(new AboutFundDataSlotViewModel { Label = "YTD", Status = AboutFundFetchStatus.Succeeded });
        DataSlots.Add(new AboutFundDataSlotViewModel { Label = "1Y", Status = AboutFundFetchStatus.Succeeded });
        DataSlots.Add(new AboutFundDataSlotViewModel { Label = "3Y", Status = AboutFundFetchStatus.Pending });
        DataSlots.Add(new AboutFundDataSlotViewModel { Label = "5Y", Status = AboutFundFetchStatus.Pending });
        DataSlots.Add(new AboutFundDataSlotViewModel { Label = "Max", Status = AboutFundFetchStatus.Pending });

        CollectionElapsed = TimeSpan.FromSeconds(4.8);
        CollectionRemaining = TimeSpan.FromSeconds(10.2);
    }

    /// <summary>
    /// Called when session state changes (every ~1 second from orchestrator).
    /// </summary>
    /// <param name="state">The new session state.</param>
    public void OnSessionStateChanged(AboutFundSessionState state)
    {
        IsSessionActive = state.IsActive;
        TotalFunds = state.TotalFunds;
        StatusMessage = state.StatusMessage;
        IsDelayInProgress = state.IsDelayInProgress;
        DelayCountdown = state.DelayCountdown;
        EstimatedTimeRemaining = state.EstimatedTimeRemaining;
        CurrentFundName = state.CurrentFundName;
        CurrentIsin = state.CurrentIsin;
        CurrentOrderBookId = state.CurrentOrderBookId?.Value;

        ProjectCollectionProgress(state.CollectionProgress);

        CommandManager.InvalidateRequerySuggested();
    }

    #region Collection Progress Projection

    private void ProjectCollectionProgress(AboutFundCollectionProgress? progress)
    {
        IsCollecting = progress != null && !IsDelayInProgress;

        if (progress == null)
        {
            ClearCollectionState();
            return;
        }

        // Update timing
        CollectionElapsed = progress.Elapsed;
        CollectionRemaining = progress.Remaining;

        // Project steps — in-place update to avoid flicker
        ProjectSteps(progress.Steps);

        // Project data slots — in-place update
        ProjectDataSlots(progress.PageData);
    }

    private void ProjectSteps(IReadOnlyList<AboutFundCollectionStep> steps)
    {
        TotalStepsCount = steps.Count;

        // Find the first pending step (the "current" one being waited on)
        var currentStepIndex = -1;
        var completedCount = 0;
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Status != AboutFundCollectionStepStatus.Pending)
                completedCount++;
            else if (currentStepIndex < 0)
                currentStepIndex = i;
        }

        CompletedStepsCount = completedCount;

        // Ensure collection has right number of VMs
        EnsureCollectionSize(CollectionSteps, steps.Count);

        for (var i = 0; i < steps.Count; i++)
            CollectionSteps[i].Update(steps[i], isCurrent: i == currentStepIndex);
    }

    private void ProjectDataSlots(AboutFundPageData pageData)
    {
        TotalSlotsCount = pageData.TotalSlots;
        ResolvedSlotsCount = pageData.ResolvedCount;

        var slots = pageData.AllSlots().ToList();

        // Ensure collection has right number of VMs
        EnsureCollectionSize(DataSlots, slots.Count);

        for (var i = 0; i < slots.Count; i++)
            DataSlots[i].Update(slots[i].Slot, slots[i].Data);
    }

    private void ClearCollectionState()
    {
        CollectionSteps.Clear();
        DataSlots.Clear();
        CompletedStepsCount = 0;
        TotalStepsCount = 0;
        ResolvedSlotsCount = 0;
        TotalSlotsCount = 0;
        CollectionElapsed = TimeSpan.Zero;
        CollectionRemaining = TimeSpan.Zero;
    }

    private static void EnsureCollectionSize<T>(ObservableCollection<T> collection, int targetCount)
        where T : new()
    {
        while (collection.Count < targetCount)
            collection.Add(new T());

        while (collection.Count > targetCount)
            collection.RemoveAt(collection.Count - 1);
    }

    #endregion
}
