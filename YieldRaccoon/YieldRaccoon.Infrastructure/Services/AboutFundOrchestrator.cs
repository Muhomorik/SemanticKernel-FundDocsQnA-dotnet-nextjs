using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events.AboutFund;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Models;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Orchestrates about-fund browsing session lifecycle, navigation workflow, and timer management.
/// </summary>
/// <remarks>
/// <para>
/// This service manages the workflow of navigating through fund detail pages:
/// <list type="bullet">
///   <item>Session lifecycle (start, cancel, complete)</item>
///   <item>Delegates schedule calculation to <see cref="IAboutFundScheduleCalculator"/></item>
///   <item>Schedules all fund visit timers upfront in <see cref="StartSessionAsync"/></item>
///   <item>Event publishing to about-fund event store</item>
///   <item>State projection from events and collector progress to observable streams</item>
/// </list>
/// </para>
/// <para>
/// <strong>Phase lifecycle:</strong>
/// <c>Idle → DelayBeforeNavigation → Collecting → DelayBeforeNavigation → … → Idle</c>.
/// </para>
/// </remarks>
public class AboutFundOrchestrator : IAboutFundOrchestrator
{
    private readonly ILogger _logger;
    private readonly IAboutFundEventStore _eventStore;
    private readonly IFundProfileRepository _fundProfileRepository;
    private readonly IAboutFundPageDataCollector _collector;
    private readonly IAboutFundChartIngestionService _chartIngestionService;
    private readonly IPortfolioDataIngestionService _portfolioDataIngestionService;
    private readonly IFundDetailsUrlBuilder _urlBuilder;
    private readonly IAboutFundPageInteractor _pageInteractor;
    private readonly IAboutFundScheduleCalculator _scheduleCalculator;

    private readonly IScheduler _scheduler;
    private readonly CompositeDisposable _disposables = new();

    #region Session state

    /// <summary>Unique correlation ID for the active session.</summary>
    private AboutFundSessionId? _currentSessionId;

    /// <summary>
    /// Ordered list of funds to visit in this session (for metadata like Name).
    /// </summary>
    private IReadOnlyList<AboutFundScheduleItem> _schedule = Array.Empty<AboutFundScheduleItem>();

    /// <summary>
    /// Pre-calculated timing for every fund in the session (ordered).
    /// </summary>
    private List<AboutFundCollectionSchedule> _fundSchedules = [];

    /// <summary>
    /// Per-fund visit status — authoritative state for skip/advance logic.
    /// </summary>
    private readonly Dictionary<OrderBookId, FundVisitStatus> _visitStatuses = new();

    /// <summary>
    /// Explicit lifecycle phase — replaces implicit boolean flags.
    /// </summary>
    private AboutFundSessionPhase _phase;

    /// <summary>
    /// Identity of the fund currently being visited (or about to be visited after delay).
    /// </summary>
    private OrderBookId? _currentOrderBookId;

    /// <summary>
    /// Controls whether the next fund is auto-scheduled after a collection completes.
    /// </summary>
    private bool _autoAdvanceEnabled;

    /// <summary>
    /// All scheduled fund visit timers + 1s ticker.
    /// Disposed on cancel, advance, or auto-advance toggle.
    /// </summary>
    private CompositeDisposable? _scheduledVisits;

    /// <summary>
    /// Latest progress from the collector's <see cref="IAboutFundPageDataCollector.StateChanged"/>.
    /// Merged into <see cref="ProjectState"/> during the <see cref="AboutFundSessionPhase.Collecting"/> phase.
    /// </summary>
    private AboutFundCollectionProgress? _latestCollectionProgress;

    /// <summary>
    /// ISIN resolved for the manually-navigated fund. Used for per-slot persistence in manual mode.
    /// </summary>
    private IsinId? _manualIsinId;

    /// <summary>
    /// Fund name resolved during manual collection, for display purposes.
    /// </summary>
    private string? _manualFundName;

    /// <summary>
    /// Subscription to <see cref="IAboutFundPageDataCollector.SlotUpdated"/> for per-slot persistence
    /// in manual collection mode. Disposed when manual mode ends.
    /// </summary>
    private IDisposable? _manualSlotSubscription;

    private bool _disposed;

    #endregion

    // BehaviorSubjects for state (emit current value to new subscribers)
    private readonly BehaviorSubject<AboutFundSessionState> _sessionState;

    /// <inheritdoc/>
    public IObservable<AboutFundSessionState> SessionState => _sessionState.AsObservable();

    // Subjects for events (no initial value)
    private readonly Subject<IAboutFundEvent> _events = new();

    /// <inheritdoc/>
    public IObservable<IAboutFundEvent> Events => _events.AsObservable();

    private readonly Subject<Uri> _navigateToUrl = new();

    /// <inheritdoc/>
    public IObservable<Uri> NavigateToUrl => _navigateToUrl.AsObservable();

    /// <summary>
    /// 80 calls a day allows us to make a full loop in a month.
    /// </summary>
    private const int ScheduleLimit = 80;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundOrchestrator"/> class.
    /// </summary>
    public AboutFundOrchestrator(ILogger logger,
        IScheduler scheduler,
        IFundDetailsUrlBuilder urlBuilder,
        IAboutFundPageInteractor pageInteractor,
        IAboutFundPageDataCollector collector,
        IAboutFundChartIngestionService chartIngestionService,
        IPortfolioDataIngestionService portfolioDataIngestionService,
        IAboutFundEventStore eventStore,
        IFundProfileRepository fundProfileRepository,
        IAboutFundScheduleCalculator scheduleCalculator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _fundProfileRepository =
            fundProfileRepository ?? throw new ArgumentNullException(nameof(fundProfileRepository));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        _chartIngestionService =
            chartIngestionService ?? throw new ArgumentNullException(nameof(chartIngestionService));
        _portfolioDataIngestionService =
            portfolioDataIngestionService ?? throw new ArgumentNullException(nameof(portfolioDataIngestionService));
        _urlBuilder = urlBuilder ?? throw new ArgumentNullException(nameof(urlBuilder));
        _pageInteractor = pageInteractor ?? throw new ArgumentNullException(nameof(pageInteractor));
        _scheduleCalculator = scheduleCalculator ?? throw new ArgumentNullException(nameof(scheduleCalculator));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

        _phase = AboutFundSessionPhase.Idle;

        _sessionState = new BehaviorSubject<AboutFundSessionState>(AboutFundSessionState.Inactive);

        // Subscribe to collector completion — single DB write point
        _disposables.Add(
            _collector.Completed.Subscribe(OnPageDataCollected));

        // Subscribe to collector progress — merge into session state every second
        _disposables.Add(
            _collector.StateChanged.Subscribe(OnCollectionStateChanged));

        _logger.Debug("AboutFundOrchestrator initialized");
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AboutFundScheduleItem>> LoadScheduleAsync(int? limit = null)
    {
        var effectiveLimit = limit ?? ScheduleLimit;
        _logger.Info("Loading fund schedule from database (limit: {0})", effectiveLimit);

        _schedule = await _fundProfileRepository.GetFundsOrderedByLastVisitAsync(effectiveLimit)
            .ConfigureAwait(false);

        _logger.Info("Loaded {0} funds into schedule", _schedule.Count);
        return _schedule;
    }

    /// <inheritdoc/>
    public async Task<AboutFundSessionId> StartSessionAsync(
        IReadOnlyList<AboutFundCollectionStepKind>? enabledSteps = null)
    {
        _logger.Info("Starting new about-fund browsing session");

        // Cancel any active manual collection — automated mode takes over
        if (_phase == AboutFundSessionPhase.ManualCollecting)
        {
            _logger.Info("Cancelling active manual collection before starting automated session");
            CancelManualCollection();
        }

        // Load schedule if not already loaded
        if (_schedule.Count == 0) await LoadScheduleAsync();

        if (_schedule.Count == 0)
        {
            _logger.Warn("No funds available for browsing - schedule is empty");
            throw new InvalidOperationException("No funds available for browsing.");
        }

        _currentSessionId = AboutFundSessionId.NewId();

        // Publish session started event
        var sessionStarted = AboutFundSessionStarted.Create(
            _currentSessionId.Value,
            _schedule.Count,
            _schedule[0].OrderBookId);
        AppendAndEmit(sessionStarted);

        _logger.Info("Session {0} started with {1} funds", _currentSessionId, _schedule.Count);

        // Pre-calculate the full session schedule
        var steps = enabledSteps != null
            ? AboutFundCollectionStepKinds.ForSteps(enabledSteps)
            : AboutFundCollectionStepKinds.All;

        _fundSchedules = _scheduleCalculator.CalculateSessionSchedule(
            _schedule,
            _scheduler.Now + TimeSpan.FromSeconds(15),
            _pageInteractor.GetMinimumDelay,
            steps);

        // Initialize all funds as NotVisited
        _visitStatuses.Clear();
        foreach (var fs in _fundSchedules)
            _visitStatuses[fs.OrderBookId] = FundVisitStatus.NotVisited;

        // Set initial state — first fund is about to be visited after its delay
        _phase = AboutFundSessionPhase.DelayBeforeNavigation;
        _currentOrderBookId = _schedule[0].OrderBookId;

        // Schedule ALL fund visit timers upfront
        ScheduleVisits(_schedule[0].OrderBookId);

        RefreshState();

        return _currentSessionId.Value;
    }

    /// <inheritdoc/>
    public void CancelSession(string reason)
    {
        // Handle manual collection cancellation (no session ID in manual mode)
        if (_phase == AboutFundSessionPhase.ManualCollecting)
        {
            _logger.Info("Cancelling manual collection: {0}", reason);
            CancelManualCollection();
            RefreshState();
            return;
        }

        if (_currentSessionId == null)
        {
            _logger.Warn("CancelSession called but no session is active");
            return;
        }

        _logger.Info("Cancelling session {0}: {1}", _currentSessionId, reason);

        CancelScheduledVisits();
        _collector.CancelCollection();

        var fundsVisited = _eventStore.GetCompletedNavigationCount(_currentSessionId.Value);

        var cancelled = AboutFundSessionCancelled.Create(
            _currentSessionId.Value,
            fundsVisited,
            reason);
        AppendAndEmit(cancelled);

        _logger.Info("Session {0} cancelled after visiting {1} funds", _currentSessionId, fundsVisited);

        _currentSessionId = null;
        _currentOrderBookId = null;
        _latestCollectionProgress = null;
        _fundSchedules = [];
        _visitStatuses.Clear();
        _phase = AboutFundSessionPhase.Idle;

        RefreshState();
    }

    /// <inheritdoc/>
    public void AdvanceToNextFund()
    {
        if (_currentSessionId == null)
        {
            _logger.Warn("AdvanceToNextFund called but no session is active");
            return;
        }

        CancelScheduledVisits();

        var nextSchedule = GetNextUnvisitedSchedule(_currentOrderBookId);
        if (nextSchedule == null)
        {
            CompleteSession();
            return;
        }

        // Skip delay — immediately visit the next fund
        ExecuteFundVisit(nextSchedule);

        // Only reschedule remaining funds when auto-advance is enabled;
        // in manual mode the user controls the pace.
        if (_autoAdvanceEnabled)
        {
            var afterNext = GetNextUnvisitedSchedule(nextSchedule.OrderBookId);
            if (afterNext != null)
            {
                _fundSchedules = _scheduleCalculator.RecalculateRemainingSchedule(
                    _fundSchedules, afterNext.OrderBookId, nextSchedule.StopTime, _visitStatuses);
                ScheduleVisits(afterNext.OrderBookId);
            }
        }
    }

    /// <inheritdoc/>
    public void SetAutoAdvance(bool enabled)
    {
        _autoAdvanceEnabled = enabled;
        _logger.Info("Auto-advance {0}", enabled ? "enabled" : "disabled");
    }

    /// <inheritdoc/>
    public async Task StartManualCollectionAsync(Uri url)
    {
        _logger.Info("Manual navigation requested: {0}", url);

        // If an automated session is active, just navigate without starting manual collection
        if (_phase is AboutFundSessionPhase.Collecting or AboutFundSessionPhase.DelayBeforeNavigation)
        {
            _logger.Info("Automated session active — navigating without manual collection");
            _navigateToUrl.OnNext(url);
            return;
        }

        // Clean up any previous manual collection
        CancelManualCollection();

        // Try to parse OrderBookId from the URL
        if (!_urlBuilder.TryParseOrderBookId(url, out var orderBookId))
        {
            _logger.Info("URL does not match fund template — navigating without collection");
            _navigateToUrl.OnNext(url);
            return;
        }

        _logger.Info("Parsed OrderBookId={0} from URL", orderBookId);

        // Resolve ISIN: check loaded schedule first, then fall back to DB
        var isinString = FindIsinInSchedule(orderBookId)
                         ?? await _fundProfileRepository.GetIsinByOrderBookIdAsync(orderBookId);

        if (string.IsNullOrWhiteSpace(isinString))
        {
            _logger.Warn("No ISIN found for OrderBookId={0} — navigating without persistence", orderBookId);
            _navigateToUrl.OnNext(url);
            return;
        }

        _manualIsinId = IsinId.Create(isinString);
        _currentOrderBookId = orderBookId;
        _manualFundName = FindFundNameInSchedule(orderBookId);

        // Start passive collection (no timers, no scheduled interactions)
        _collector.BeginPassiveCollection(orderBookId);

        // Subscribe to per-slot updates for incremental persistence
        _manualSlotSubscription = _collector.SlotUpdated.Subscribe(OnManualSlotUpdated);

        _phase = AboutFundSessionPhase.ManualCollecting;

        // Seed initial progress so the control panel has slot data immediately (all pending)
        _latestCollectionProgress = new AboutFundCollectionProgress
        {
            OrderBookId = orderBookId,
            Steps = [],
            TotalDuration = TimeSpan.Zero,
            PageData = new AboutFundPageData { OrderBookId = orderBookId }
        };

        _logger.Info("Manual collection started for OrderBookId={0} (ISIN: {1})", orderBookId, isinString);

        RefreshState();
        _navigateToUrl.OnNext(url);
    }

    /// <summary>
    /// Called when a slot resolves during manual collection. Persists chart data incrementally.
    /// </summary>
    private void OnManualSlotUpdated(AboutFundPageData pageData)
    {
        if (_phase != AboutFundSessionPhase.ManualCollecting || _manualIsinId == null)
            return;

        _logger.Debug("Manual slot updated for {0} — persisting ({1}/{2} resolved)",
            pageData.OrderBookId, pageData.ResolvedCount, pageData.TotalSlots);

        PersistChartDataForManualAsync(pageData);
    }

    /// <summary>
    /// Persists chart data during manual collection using the pre-resolved ISIN.
    /// </summary>
    private async void PersistChartDataForManualAsync(AboutFundPageData pageData)
    {
        try
        {
            if (_manualIsinId == null) return;

            var count = await _chartIngestionService.IngestChartDataAsync(pageData, _manualIsinId.Value);

            _logger.Info("Manual chart ingestion for {0}: {1} new records persisted",
                pageData.OrderBookId, count);

            await _fundProfileRepository.UpdateLastVisitedAtAsync(_manualIsinId.Value, DateTimeOffset.UtcNow);

            await PersistFundDescriptionAsync(pageData, _manualIsinId.Value);

            var allocCount = await _portfolioDataIngestionService.IngestPortfolioDataAsync(
                pageData, _manualIsinId.Value);
            if (allocCount > 0)
                _logger.Info("Manual portfolio ingestion for {0}: {1} allocation rows touched",
                    pageData.OrderBookId, allocCount);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to persist manual chart data for {0}", pageData.OrderBookId);
        }
    }

    /// <summary>
    /// Cleans up manual collection state and subscriptions.
    /// </summary>
    private void CancelManualCollection()
    {
        _manualSlotSubscription?.Dispose();
        _manualSlotSubscription = null;
        _manualIsinId = null;
        _manualFundName = null;

        if (_phase == AboutFundSessionPhase.ManualCollecting)
        {
            _collector.CancelCollection();
            _currentOrderBookId = null;
            _latestCollectionProgress = null;
            _phase = AboutFundSessionPhase.Idle;
        }
    }

    /// <summary>
    /// Searches the loaded schedule for an ISIN matching the given OrderBookId.
    /// </summary>
    private string? FindIsinInSchedule(OrderBookId orderBookId)
    {
        var index = GetScheduleIndex(orderBookId);
        return index >= 0 ? _schedule[index].Isin : null;
    }

    /// <summary>
    /// Searches the loaded schedule for a fund name matching the given OrderBookId.
    /// </summary>
    private string? FindFundNameInSchedule(OrderBookId orderBookId)
    {
        var index = GetScheduleIndex(orderBookId);
        return index >= 0 ? _schedule[index].Name : null;
    }

    #region Navigation scheduling

    /// <summary>
    /// Schedules <see cref="Observable.Timer"/> for each unvisited fund starting from
    /// <paramref name="fromOrderBookId"/> at the pre-calculated start times.
    /// Includes a 1-second ticker for UI progress.
    /// </summary>
    private void ScheduleVisits(OrderBookId fromOrderBookId)
    {
        CancelScheduledVisits();

        var disposables = new CompositeDisposable();
        var now = _scheduler.Now;
        var startIndex = GetFundScheduleIndex(fromOrderBookId);

        for (var i = startIndex; i < _fundSchedules.Count; i++)
        {
            var entry = _fundSchedules[i];

            // Skip already visited or in-progress funds
            if (_visitStatuses.TryGetValue(entry.OrderBookId, out var status)
                && status != FundVisitStatus.NotVisited)
                continue;

            var delay = entry.StartTime - now;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            var capturedEntry = entry;
            disposables.Add(Observable.Timer(delay, _scheduler)
                .Subscribe(_ => ExecuteFundVisit(capturedEntry)));
        }

        // 1-second ticker for delay countdown and progress display
        disposables.Add(Observable.Interval(TimeSpan.FromSeconds(1), _scheduler)
            .Subscribe(_ => RefreshState()));

        _scheduledVisits = disposables;
    }

    /// <summary>
    /// Immediately navigates to a fund detail page and begins data collection
    /// using the pre-calculated step timings.
    /// </summary>
    private void ExecuteFundVisit(AboutFundCollectionSchedule fundSchedule)
    {
        if (_currentSessionId == null) return;

        _phase = AboutFundSessionPhase.Collecting;
        _currentOrderBookId = fundSchedule.OrderBookId;
        _visitStatuses[fundSchedule.OrderBookId] = FundVisitStatus.Collecting;
        _latestCollectionProgress = null;

        // Begin data collection with pre-calculated step timings
        var progressSnapshot = _collector.BeginCollection(fundSchedule);
        _latestCollectionProgress = progressSnapshot;

        var url = _urlBuilder.BuildUrl(fundSchedule.OrderBookId);
        var index = GetScheduleIndex(fundSchedule.OrderBookId);
        var isin = index >= 0 ? _schedule[index].Isin : string.Empty;

        var navStarted = AboutFundNavigationStarted.Create(
            _currentSessionId.Value,
            isin,
            fundSchedule.OrderBookId,
            url.ToString());
        AppendAndEmit(navStarted);

        RefreshState();

        var fundName = index >= 0 ? _schedule[index].Name : isin;
        _logger.Info("Navigating to fund {0}/{1}: {2} ({3})",
            index + 1, _schedule.Count, fundName, url);

        _navigateToUrl.OnNext(url);
    }

    /// <summary>
    /// Cancels all pending fund visit timers and the progress ticker.
    /// </summary>
    private void CancelScheduledVisits()
    {
        _scheduledVisits?.Dispose();
        _scheduledVisits = null;
    }

    #endregion

    #region Session lifecycle

    /// <summary>
    /// Completes the current session.
    /// </summary>
    private void CompleteSession()
    {
        if (_currentSessionId == null) return;

        CancelScheduledVisits();
        _collector.CancelCollection();

        var sessionId = _currentSessionId.Value;
        var fundsVisited = _eventStore.GetCompletedNavigationCount(sessionId);
        var activeSession = _eventStore.GetActiveSession();
        var startedAt = activeSession?.OccurredAt ?? DateTimeOffset.UtcNow;

        var completed = AboutFundSessionCompleted.Create(sessionId, fundsVisited, startedAt);
        AppendAndEmit(completed);

        _logger.Info("Session {0} completed - visited {1} funds", sessionId, fundsVisited);

        _currentSessionId = null;
        _currentOrderBookId = null;
        _latestCollectionProgress = null;
        _fundSchedules = [];
        _visitStatuses.Clear();
        _phase = AboutFundSessionPhase.Idle;

        RefreshState();
    }

    #endregion

    #region Collector event handlers

    /// <summary>
    /// Called when the collector has all slots resolved for a fund page visit.
    /// Persists chart data via <see cref="IAboutFundChartIngestionService"/>,
    /// publishes <see cref="AboutFundNavigationCompleted"/>, and transitions to
    /// delay phase for the next fund (its timer is already scheduled).
    /// </summary>
    private void OnPageDataCollected(AboutFundPageData pageData)
    {
        _logger.Info("Page data collected for {0}: {1} (succeeded={2}, total={3})",
            pageData.OrderBookId,
            pageData.IsFullySuccessful ? "full" : "partial",
            pageData.ResolvedCount - (pageData.IsFullySuccessful ? 0 : 1),
            pageData.TotalSlots);

        PersistChartDataAsync(pageData);

        if (_currentSessionId == null)
            return;

        // Only act on the currently active fund.
        // Force-completed previous funds (from BeginCollection) should not trigger advance.
        if (pageData.OrderBookId != _currentOrderBookId)
            return;

        var index = GetScheduleIndex(pageData.OrderBookId);
        var fund = index >= 0 ? _schedule[index] : null;

        _visitStatuses[pageData.OrderBookId] = FundVisitStatus.Completed;

        var navigationCompleted = AboutFundNavigationCompleted.Create(
            _currentSessionId.Value,
            fund?.Isin ?? string.Empty,
            pageData.OrderBookId);
        AppendAndEmit(navigationCompleted);

        _latestCollectionProgress = null;

        var nextSchedule = GetNextUnvisitedSchedule(pageData.OrderBookId);
        if (nextSchedule == null)
        {
            CompleteSession();
            return;
        }

        // Transition to delay phase — the next fund's timer is already scheduled
        _phase = AboutFundSessionPhase.DelayBeforeNavigation;
        _currentOrderBookId = nextSchedule.OrderBookId;

        RefreshState();
    }

    /// <summary>
    /// Called every second by the collector with updated progress.
    /// Merges the collection schedule into the session state stream.
    /// </summary>
    private void OnCollectionStateChanged(AboutFundCollectionProgress progress)
    {
        _latestCollectionProgress = progress;
        RefreshState();
    }

    /// <summary>
    /// Persists chart data from a completed page visit via the chart ingestion service.
    /// Resolves the fund's ISIN from the session schedule and delegates to
    /// <see cref="IAboutFundChartIngestionService"/>.
    /// </summary>
    /// <remarks>
    /// Called fire-and-forget from the synchronous Rx callback.
    /// Errors are caught and logged — a persistence failure must not break the browsing session.
    /// </remarks>
    private async void PersistChartDataAsync(AboutFundPageData pageData)
    {
        try
        {
            var index = GetScheduleIndex(pageData.OrderBookId);
            if (index < 0)
            {
                _logger.Warn("Cannot persist chart data for {0}: not found in schedule",
                    pageData.OrderBookId);
                return;
            }

            var isinString = _schedule[index].Isin;
            if (string.IsNullOrWhiteSpace(isinString))
            {
                _logger.Warn("Cannot persist chart data for {0}: ISIN is empty",
                    pageData.OrderBookId);
                return;
            }

            var isinId = IsinId.Create(isinString);
            var count = await _chartIngestionService.IngestChartDataAsync(pageData, isinId);

            _logger.Info("Chart ingestion complete for {0}: {1} records persisted",
                pageData.OrderBookId, count);

            // Update the "last visited" timestamp on the fund profile
            await _fundProfileRepository.UpdateLastVisitedAtAsync(isinId, DateTimeOffset.UtcNow);
            _logger.Debug("Updated AboutFundLastVisitedAt for {0} (ISIN: {1})",
                pageData.OrderBookId, isinId.Isin);

            await PersistFundDescriptionAsync(pageData, isinId);

            var allocCount = await _portfolioDataIngestionService.IngestPortfolioDataAsync(pageData, isinId);
            if (allocCount > 0)
                _logger.Info("Portfolio ingestion for {0}: {1} allocation rows touched",
                    pageData.OrderBookId, allocCount);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to persist chart data for {0}", pageData.OrderBookId);
        }
    }

    /// <summary>
    /// Extracts the fund description from the captured fund-reference JSON
    /// and persists it to the fund profile.
    /// </summary>
    private async Task PersistFundDescriptionAsync(AboutFundPageData pageData, IsinId isinId)
    {
        if (string.IsNullOrWhiteSpace(pageData.FundReferenceJson))
            return;

        try
        {
            var description = JsonSerializer
                .Deserialize<FundReferenceResponse>(pageData.FundReferenceJson)?.Description;

            if (string.IsNullOrWhiteSpace(description))
                return;

            await _fundProfileRepository.UpdateDescriptionAsync(isinId, description);
            _logger.Debug("Updated description for {0} (ISIN: {1})",
                pageData.OrderBookId, isinId.Isin);
        }
        catch (JsonException ex)
        {
            _logger.Warn(ex, "Failed to deserialize fund-reference JSON for {0}", pageData.OrderBookId);
        }
    }

    #endregion

    #region State projection

    /// <summary>
    /// Appends an event to the store and emits it on the Events observable.
    /// </summary>
    private void AppendAndEmit(IAboutFundEvent aboutFundEvent)
    {
        _eventStore.Append(aboutFundEvent);
        _events.OnNext(aboutFundEvent);
    }

    /// <summary>
    /// Refreshes session state and emits update.
    /// </summary>
    private void RefreshState()
    {
        var state = ProjectState();
        _sessionState.OnNext(state);
    }

    /// <summary>
    /// Projects current session state from tracking fields, pre-calculated schedule,
    /// and collector progress.
    /// </summary>
    private AboutFundSessionState ProjectState()
    {
        // Manual collection mode — no session ID, minimal state
        if (_phase == AboutFundSessionPhase.ManualCollecting)
            return new AboutFundSessionState
            {
                IsActive = true,
                Phase = AboutFundSessionPhase.ManualCollecting,
                CurrentOrderBookId = _currentOrderBookId,
                CurrentFundName = _manualFundName,
                CurrentIsin = _manualIsinId?.Isin,
                StatusMessage = _manualFundName != null
                    ? $"Manual: {_manualFundName}"
                    : "Manual collection",
                CollectionProgress = _latestCollectionProgress
            };

        if (_currentSessionId == null || _phase == AboutFundSessionPhase.Idle)
            return AboutFundSessionState.Inactive;

        var sessionId = _currentSessionId.Value;
        var completedCount = _eventStore.GetCompletedNavigationCount(sessionId);
        var currentIndex = _currentOrderBookId.HasValue
            ? GetScheduleIndex(_currentOrderBookId.Value)
            : -1;
        var currentFundName = currentIndex >= 0 ? _schedule[currentIndex].Name : null;

        var statusMessage = currentFundName != null
            ? $"Viewing {currentIndex + 1}/{_schedule.Count}: {currentFundName}"
            : $"Completed {completedCount}/{_schedule.Count} funds";

        // Delay countdown from a pre-calculated schedule
        var delayCountdown = 0;
        if (_phase == AboutFundSessionPhase.DelayBeforeNavigation
            && _currentOrderBookId.HasValue)
        {
            var scheduleEntry = GetFundScheduleByOrderBookId(_currentOrderBookId.Value);
            if (scheduleEntry != null)
            {
                var remaining = scheduleEntry.StartTime - _scheduler.Now;
                delayCountdown = Math.Max(0, (int)remaining.TotalSeconds);
                statusMessage = $"Next fund in {delayCountdown}s...";
            }
        }

        // Accurate ETA from pre-calculated session schedule
        var estimatedTimeRemaining = TimeSpan.Zero;
        if (currentIndex >= 0)
        {
            // Remaining time for the current fund
            if (_phase == AboutFundSessionPhase.Collecting && _latestCollectionProgress != null)
                estimatedTimeRemaining += _latestCollectionProgress.Remaining;
            else if (_phase == AboutFundSessionPhase.DelayBeforeNavigation
                     && currentIndex < _fundSchedules.Count)
                estimatedTimeRemaining += _fundSchedules[currentIndex].TotalDuration;

            // Sum remaining unvisited funds' delays + collection durations
            for (var i = currentIndex + 1; i < _fundSchedules.Count; i++)
            {
                if (_visitStatuses.TryGetValue(_fundSchedules[i].OrderBookId, out var s)
                    && s == FundVisitStatus.Completed)
                    continue;
                estimatedTimeRemaining += _fundSchedules[i].InterPageDelay
                                          + _fundSchedules[i].TotalDuration;
            }
        }

        return new AboutFundSessionState
        {
            IsActive = _phase is AboutFundSessionPhase.DelayBeforeNavigation
                or AboutFundSessionPhase.Collecting,
            Phase = _phase,
            SessionId = sessionId,
            CurrentOrderBookId = _currentOrderBookId,
            TotalFunds = _schedule.Count,
            CurrentIsin = currentIndex >= 0 ? _schedule[currentIndex].Isin : null,
            CurrentFundName = currentFundName,
            StatusMessage = statusMessage,
            IsDelayInProgress = _phase == AboutFundSessionPhase.DelayBeforeNavigation,
            DelayCountdown = delayCountdown,
            EstimatedTimeRemaining = estimatedTimeRemaining,
            CollectionProgress = _latestCollectionProgress
        };
    }

    #endregion

    #region Schedule helpers

    /// <summary>
    /// Returns zero-based position of an <see cref="OrderBookId"/> in the fund schedule, or <c>-1</c>.
    /// </summary>
    private int GetScheduleIndex(OrderBookId orderBookId)
    {
        for (var i = 0; i < _schedule.Count; i++)
            if (_schedule[i].OrderBookId == orderBookId)
                return i;

        return -1;
    }

    /// <summary>
    /// Returns zero-based position of an <see cref="OrderBookId"/> in the pre-calculated fund schedules.
    /// </summary>
    private int GetFundScheduleIndex(OrderBookId orderBookId)
    {
        for (var i = 0; i < _fundSchedules.Count; i++)
            if (_fundSchedules[i].OrderBookId == orderBookId)
                return i;

        return -1;
    }

    /// <summary>
    /// Returns the <see cref="AboutFundCollectionSchedule"/> for the given OrderBookId, or null.
    /// </summary>
    private AboutFundCollectionSchedule? GetFundScheduleByOrderBookId(OrderBookId orderBookId)
    {
        var index = GetFundScheduleIndex(orderBookId);
        return index >= 0 ? _fundSchedules[index] : null;
    }

    /// <summary>
    /// Returns the next <see cref="AboutFundCollectionSchedule"/> after the given one
    /// that has <see cref="FundVisitStatus.NotVisited"/> status, or null if none remain.
    /// </summary>
    private AboutFundCollectionSchedule? GetNextUnvisitedSchedule(OrderBookId? orderBookId)
    {
        if (!orderBookId.HasValue) return null;

        var index = GetFundScheduleIndex(orderBookId.Value);
        if (index < 0) return null;

        for (var i = index + 1; i < _fundSchedules.Count; i++)
        {
            var id = _fundSchedules[i].OrderBookId;
            if (!_visitStatuses.TryGetValue(id, out var status)
                || status == FundVisitStatus.NotVisited)
                return _fundSchedules[i];
        }

        return null;
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.Debug("AboutFundOrchestrator disposing");

        CancelManualCollection();
        CancelScheduledVisits();
        _collector.CancelCollection();
        _disposables.Dispose();
        _sessionState.Dispose();
        _events.Dispose();
        _navigateToUrl.Dispose();

        _phase = AboutFundSessionPhase.Idle;
        _disposed = true;
    }
}
