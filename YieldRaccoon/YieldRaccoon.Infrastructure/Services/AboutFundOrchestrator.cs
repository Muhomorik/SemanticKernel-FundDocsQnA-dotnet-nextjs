using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events.AboutFund;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Orchestrates about-fund browsing session lifecycle, navigation workflow, and timer management.
/// </summary>
/// <remarks>
/// <para>
/// This service manages the workflow of navigating through fund detail pages:
/// <list type="bullet">
///   <item>Session lifecycle (start, cancel, complete)</item>
///   <item>Fund navigation sequencing</item>
///   <item>Auto-advance timer management using Rx.NET</item>
///   <item>Event publishing to about-fund event store</item>
///   <item>State projection from events to observable streams</item>
/// </list>
/// </para>
/// </remarks>
public class AboutFundOrchestrator : IAboutFundOrchestrator
{
    private readonly ILogger _logger;
    private readonly IAboutFundEventStore _eventStore;
    private readonly IFundProfileRepository _fundProfileRepository;
    private readonly IAboutFundPageInteractor _pageInteractor;
    private readonly IAboutFundPageDataCollector _collector;
    private readonly AboutFundResponseParser _responseParser;
    private readonly IFundDetailsUrlBuilder _urlBuilder;
    private readonly IScheduler _scheduler;
    private readonly CompositeDisposable _disposables = new();

    // Current session tracking
    private AboutFundSessionId? _currentSessionId;
    private IReadOnlyList<AboutFundScheduleItem> _schedule = Array.Empty<AboutFundScheduleItem>();
    private int _currentIndex;
    private bool _autoAdvanceEnabled;
    private IDisposable? _autoAdvanceSubscription;
    private bool _disposed;

    // Auto-advance delay in seconds
    private const int AutoAdvanceDelaySeconds = 22;

    // BehaviorSubjects for state (emit current value to new subscribers)
    private readonly BehaviorSubject<AboutFundSessionState> _sessionState;

    // Subjects for events (no initial value)
    private readonly Subject<IAboutFundEvent> _events = new();
    private readonly Subject<Uri> _navigateToUrl = new();
    private readonly Subject<int> _countdownTick = new();

    /// <inheritdoc/>
    public IObservable<AboutFundSessionState> SessionState => _sessionState.AsObservable();

    /// <inheritdoc/>
    public IObservable<IAboutFundEvent> Events => _events.AsObservable();

    /// <inheritdoc/>
    public IObservable<Uri> NavigateToUrl => _navigateToUrl.AsObservable();

    /// <inheritdoc/>
    public IObservable<int> CountdownTick => _countdownTick.AsObservable();

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundOrchestrator"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="eventStore">Event store for browsing session events.</param>
    /// <param name="fundProfileRepository">Repository for querying fund profiles.</param>
    /// <param name="pageInteractor">Page interactor for post-navigation element clicks.</param>
    /// <param name="collector">Collector for accumulating per-fund page data.</param>
    /// <param name="urlBuilder">Builds fund detail page URLs from OrderBookId.</param>
    /// <param name="scheduler">Rx scheduler for timer operations.</param>
    /// <param name="parserOptions">Endpoint patterns for response routing.</param>
    public AboutFundOrchestrator(
        ILogger logger,
        IAboutFundEventStore eventStore,
        IFundProfileRepository fundProfileRepository,
        IAboutFundPageInteractor pageInteractor,
        IAboutFundPageDataCollector collector,
        IFundDetailsUrlBuilder urlBuilder,
        IScheduler scheduler,
        ResponseParserOptions parserOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _fundProfileRepository =
            fundProfileRepository ?? throw new ArgumentNullException(nameof(fundProfileRepository));
        _pageInteractor = pageInteractor ?? throw new ArgumentNullException(nameof(pageInteractor));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        _urlBuilder = urlBuilder ?? throw new ArgumentNullException(nameof(urlBuilder));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

        _responseParser = new AboutFundResponseParser(logger, _collector, parserOptions);

        _sessionState = new BehaviorSubject<AboutFundSessionState>(AboutFundSessionState.Inactive);

        // Subscribe to collector completion — single DB write point
        _disposables.Add(
            _collector.Completed.Subscribe(OnPageDataCollected));

        _logger.Debug("AboutFundOrchestrator initialized");
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AboutFundScheduleItem>> LoadScheduleAsync()
    {
        _logger.Info("Loading fund schedule from database");
        _schedule = await _fundProfileRepository.GetFundsOrderedByHistoryCountAsync(60);
        _logger.Info("Loaded {0} funds into schedule", _schedule.Count);
        return _schedule;
    }

    /// <inheritdoc/>
    public async Task<AboutFundSessionId> StartSessionAsync()
    {
        _logger.Info("Starting new about-fund browsing session");

        // Load schedule if not already loaded
        if (_schedule.Count == 0) await LoadScheduleAsync();

        if (_schedule.Count == 0)
        {
            _logger.Warn("No funds available for browsing - schedule is empty");
            throw new InvalidOperationException("No funds available for browsing.");
        }

        _currentSessionId = AboutFundSessionId.NewId();
        _currentIndex = 0;

        var firstFund = _schedule[0];
        var firstOrderBookId = firstFund.OrderbookId ?? string.Empty;

        // Publish session started event
        var sessionStarted = AboutFundSessionStarted.Create(
            _currentSessionId.Value,
            _schedule.Count,
            firstOrderBookId);
        AppendAndEmit(sessionStarted);

        _logger.Info("Session {0} started with {1} funds", _currentSessionId, _schedule.Count);

        // Navigate to first fund
        NavigateToFund(0);

        return _currentSessionId.Value;
    }

    /// <inheritdoc/>
    public void CancelSession(string reason)
    {
        if (_currentSessionId == null)
        {
            _logger.Warn("CancelSession called but no session is active");
            return;
        }

        _logger.Info("Cancelling session {0}: {1}", _currentSessionId, reason);

        StopAutoAdvanceTimer();

        var fundsVisited = _eventStore.GetCompletedNavigationCount(_currentSessionId.Value);

        var cancelled = AboutFundSessionCancelled.Create(
            _currentSessionId.Value,
            fundsVisited,
            reason);
        AppendAndEmit(cancelled);

        _logger.Info("Session {0} cancelled after visiting {1} funds", _currentSessionId, fundsVisited);
        _currentSessionId = null;

        RefreshState();
    }

    /// <inheritdoc/>
    public void NotifyNavigationCompleted()
    {
        // Post-navigation: wait for page to render, then click "Utvecklingen i SEK" checkbox.
        // If the click fails, mark the SekPerformance slot as failed so collection can complete.
        var postNavSubscription = Observable
            .Timer(TimeSpan.FromSeconds(15), _scheduler)
            .SelectMany(_ => Observable.FromAsync(() => _pageInteractor.ActivateSekViewAsync()))
            .Subscribe(
                clicked =>
                {
                    _logger.Debug("Post-nav interaction: {0}", clicked ? "checkbox clicked" : "skipped");
                    if (!clicked)
                        _collector.FailSlot(
                            nameof(AboutFundPageData.SekPerformance),
                            "Page interaction failed — settings button or checkbox not found");
                },
                ex =>
                {
                    _logger.Warn(ex, "Post-nav interaction failed");
                    _collector.FailSlot(
                        nameof(AboutFundPageData.SekPerformance),
                        $"Page interaction exception: {ex.Message}");
                });
        _disposables.Add(postNavSubscription);

        if (_currentSessionId == null || _currentIndex >= _schedule.Count)
            return;

        var fund = _schedule[_currentIndex];

        var completed = AboutFundNavigationCompleted.Create(
            _currentSessionId.Value,
            fund.Isin,
            fund.OrderbookId ?? string.Empty,
            _currentIndex);
        AppendAndEmit(completed);

        _logger.Debug("Navigation completed for fund {0} ({1}) at index {2}",
            fund.Name, fund.Isin, _currentIndex);

        RefreshState();

        // If auto-advance is enabled, start the timer
        if (_autoAdvanceEnabled) StartAutoAdvanceTimer();
    }

    /// <inheritdoc/>
    public void AdvanceToNextFund()
    {
        if (_currentSessionId == null)
        {
            _logger.Warn("AdvanceToNextFund called but no session is active");
            return;
        }

        StopAutoAdvanceTimer();

        // Mark current as completed if not already
        var completedCount = _eventStore.GetCompletedNavigationCount(_currentSessionId.Value);
        if (completedCount <= _currentIndex) NotifyNavigationCompleted();

        var nextIndex = _currentIndex + 1;

        if (nextIndex >= _schedule.Count)
        {
            // All funds visited - complete session
            CompleteSession();
            return;
        }

        NavigateToFund(nextIndex);
    }

    /// <inheritdoc/>
    public void SetAutoAdvance(bool enabled)
    {
        _autoAdvanceEnabled = enabled;
        _logger.Info("Auto-advance {0}", enabled ? "enabled" : "disabled");

        if (!enabled) StopAutoAdvanceTimer();
    }

    /// <summary>
    /// Navigates to a fund at the given index in the schedule.
    /// </summary>
    private void NavigateToFund(int index)
    {
        if (index >= _schedule.Count || _currentSessionId == null) return;

        _currentIndex = index;
        var fund = _schedule[index];
        var orderBookId = fund.OrderbookId;

        if (string.IsNullOrWhiteSpace(orderBookId))
        {
            _logger.Warn("Fund {0} ({1}) has no OrderbookId - skipping", fund.Name, fund.Isin);
            var failed = AboutFundNavigationFailed.Create(
                _currentSessionId.Value,
                fund.Isin,
                "No OrderBookId available");
            AppendAndEmit(failed);

            // Skip to next
            var nextIndex = index + 1;
            if (nextIndex < _schedule.Count)
                NavigateToFund(nextIndex);
            else
                CompleteSession();
            return;
        }

        // Begin data collection for this fund (abandons any previous incomplete collection)
        _collector.BeginCollection(orderBookId);

        var url = _urlBuilder.BuildUrl(orderBookId);

        var navStarted = AboutFundNavigationStarted.Create(
            _currentSessionId.Value,
            fund.Isin,
            orderBookId,
            index,
            url.ToString());
        AppendAndEmit(navStarted);

        RefreshState();

        _logger.Info("Navigating to fund {0}/{1}: {2} ({3})",
            index + 1, _schedule.Count, fund.Name, url);

        _navigateToUrl.OnNext(url);
    }

    /// <summary>
    /// Completes the current session.
    /// </summary>
    private void CompleteSession()
    {
        if (_currentSessionId == null) return;

        StopAutoAdvanceTimer();

        var sessionId = _currentSessionId.Value;
        var fundsVisited = _eventStore.GetCompletedNavigationCount(sessionId);
        var activeSession = _eventStore.GetActiveSession();
        var startedAt = activeSession?.OccurredAt ?? DateTimeOffset.UtcNow;

        var completed = AboutFundSessionCompleted.Create(sessionId, fundsVisited, startedAt);
        AppendAndEmit(completed);

        _logger.Info("Session {0} completed - visited {1} funds", sessionId, fundsVisited);
        _currentSessionId = null;

        RefreshState();
    }

    /// <summary>
    /// Starts the auto-advance countdown timer, emitting countdown ticks every second.
    /// </summary>
    private void StartAutoAdvanceTimer()
    {
        StopAutoAdvanceTimer();

        _logger.Debug("Starting auto-advance countdown ({0}s)", AutoAdvanceDelaySeconds);

        _autoAdvanceSubscription = Observable
            .Interval(TimeSpan.FromSeconds(1), _scheduler)
            .Take(AutoAdvanceDelaySeconds)
            .Subscribe(
                tick =>
                {
                    var remaining = AutoAdvanceDelaySeconds - (int)tick - 1;
                    _countdownTick.OnNext(remaining);
                    EmitDelayState(remaining);
                },
                () =>
                {
                    _logger.Debug("Auto-advance countdown complete - advancing to next fund");
                    AdvanceToNextFund();
                });

        _disposables.Add(_autoAdvanceSubscription);
    }

    /// <summary>
    /// Emits session state with delay-in-progress flag and countdown value.
    /// </summary>
    private void EmitDelayState(int secondsRemaining)
    {
        var baseState = ProjectState();
        var state = baseState with
        {
            IsDelayInProgress = true,
            DelayCountdown = secondsRemaining,
            StatusMessage = $"Next fund in {secondsRemaining}s..."
        };
        _sessionState.OnNext(state);
    }

    /// <summary>
    /// Stops the auto-advance timer.
    /// </summary>
    private void StopAutoAdvanceTimer()
    {
        _autoAdvanceSubscription?.Dispose();
        _autoAdvanceSubscription = null;
    }

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
    /// Projects current session state from tracking fields.
    /// </summary>
    private AboutFundSessionState ProjectState()
    {
        if (_currentSessionId == null)
            return AboutFundSessionState.Inactive;

        var sessionId = _currentSessionId.Value;
        var completedCount = _eventStore.GetCompletedNavigationCount(sessionId);
        var currentFund = _currentIndex < _schedule.Count ? _schedule[_currentIndex] : null;

        var statusMessage = currentFund != null
            ? $"Viewing {_currentIndex + 1}/{_schedule.Count}: {currentFund.Name}"
            : $"Completed {completedCount}/{_schedule.Count} funds";

        var remainingFunds = Math.Max(0, _schedule.Count - _currentIndex - 1);
        var estimatedTimeRemaining = TimeSpan.FromSeconds(remainingFunds * AutoAdvanceDelaySeconds);

        return new AboutFundSessionState
        {
            IsActive = true,
            SessionId = sessionId,
            CurrentIndex = _currentIndex,
            TotalFunds = _schedule.Count,
            CurrentIsin = currentFund?.Isin,
            CurrentFundName = currentFund?.Name,
            StatusMessage = statusMessage,
            IsDelayInProgress = false,
            DelayCountdown = 0,
            EstimatedTimeRemaining = estimatedTimeRemaining
        };
    }

    /// <inheritdoc/>
    public void NotifyResponseCaptured(AboutFundInterceptedRequest request)
    {
        _logger.Trace("Response captured: {0} {1} -> {2}", request.Method, request.Url, request.StatusCode);
        _responseParser.TryRoute(request);
    }

    /// <summary>
    /// Called when the collector has all slots resolved for a fund page visit.
    /// This is the single write point — persist the collected data here.
    /// </summary>
    private void OnPageDataCollected(AboutFundPageData pageData)
    {
        _logger.Info("Page data collected for {0}: {1} (succeeded={2}, total={3})",
            pageData.Isin,
            pageData.IsFullySuccessful ? "full" : "partial",
            pageData.ResolvedCount - (pageData.IsFullySuccessful ? 0 : 1),
            pageData.TotalSlots);

        // TODO: Single DB write — persist pageData to repository
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.Debug("AboutFundOrchestrator disposing");

        StopAutoAdvanceTimer();
        _disposables.Dispose();
        _sessionState.Dispose();
        _events.Dispose();
        _navigateToUrl.Dispose();
        _countdownTick.Dispose();

        _disposed = true;
    }
}