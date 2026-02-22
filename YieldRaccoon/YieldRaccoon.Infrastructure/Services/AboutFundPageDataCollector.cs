using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Self-contained collector for a single fund detail page visit.
/// Owns post-navigation page interactions, response routing, slot accumulation,
/// and completion detection.
/// </summary>
/// <remarks>
/// <para>
/// Owns the in-flight <see cref="AboutFundPageData"/> for the currently visited fund.
/// When <see cref="BeginCollection"/> is called for a new fund, any previous incomplete
/// collection is force-completed and emitted on <see cref="Completed"/>.
/// </para>
/// <para>
/// <strong>Scheduling:</strong> <see cref="BeginCollection"/> receives an
/// <see cref="AboutFundCollectionSchedule"/> with pre-calculated step timings from the orchestrator.
/// It derives relative delays from the absolute fire times and schedules interaction timers using <see cref="IScheduler"/>.
/// All scheduled timers are tracked in a <see cref="CompositeDisposable"/> and cancelled
/// on abort or new collection.
/// </para>
/// <para>
/// <strong>Completion trigger:</strong> when <see cref="AboutFundCollectionStepKind.SelectMax"/>
/// succeeds, the phase transitions to <see cref="CollectionPhase.Draining"/>.
/// The next routed HTTP response then triggers <see cref="CompleteCollection"/>.
/// A safety-net timer forces completion if the response never arrives.
/// </para>
/// <para>
/// <strong>Progress reporting:</strong> a 1-second interval timer emits
/// <see cref="AboutFundCollectionProgress"/> snapshots on <see cref="StateChanged"/>
/// with elapsed/remaining time, per-step statuses, and current slot data.
/// </para>
/// <para>
/// <strong>Response routing:</strong> intercepted HTTP responses are matched against
/// configured <see cref="EndpointPattern"/>s to fill data slots on <see cref="AboutFundPageData"/>.
/// </para>
/// <para>
/// Thread-safe: all slot mutations are serialized via a lock. Observable emissions
/// happen outside the lock to avoid deadlocks with UI-thread subscribers.
/// </para>
/// </remarks>
public class AboutFundPageDataCollector : IAboutFundPageDataCollector, IDisposable
{
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;

    private readonly IAboutFundPageInteractor _pageInteractor;
    private readonly IReadOnlyList<EndpointPattern> _patterns;

    private bool _disposed;

    /// <summary>
    /// All scheduled timers for the current collection — disposed on cancel or new fund.
    /// </summary>
    private CompositeDisposable? _scheduledInteractions;

    /// <summary>
    /// Serializes all mutable state access.
    /// </summary>
    private readonly Lock _lock = new();

    /// <summary>
    /// In-flight page data accumulator for the current fund. Null when idle.
    /// </summary>
    private AboutFundPageData? _current;

    /// <summary>
    /// Current lifecycle phase (idle → interacting → draining → completed).
    /// </summary>
    private CollectionPhase _phase;

    /// <summary>
    /// Timestamp when the current collection started, for elapsed/remaining calculation.
    /// </summary>
    private DateTimeOffset _collectionStartedAt;

    /// <summary>
    /// Total scheduled duration including safety-net timer.
    /// </summary>
    private TimeSpan _totalDuration;

    /// <summary>
    /// Scheduled steps keyed by kind — statuses updated in-place as steps execute.
    /// </summary>
    private readonly Dictionary<AboutFundCollectionStepKind, AboutFundCollectionStep> _steps = [];

    private readonly Subject<AboutFundPageData> _completed = new();
    private readonly Subject<AboutFundCollectionProgress> _stateChanged = new();

    /// <inheritdoc />
    public IObservable<AboutFundPageData> Completed => _completed.AsObservable();

    /// <inheritdoc />
    public IObservable<AboutFundCollectionProgress> StateChanged => _stateChanged.AsObservable();

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundPageDataCollector"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="scheduler">Rx scheduler for timer operations.</param>
    /// <param name="parserOptions">Endpoint patterns for response routing.</param>
    /// <param name="pageInteractor">Page interactor for post-navigation element clicks.</param>
    public AboutFundPageDataCollector(ILogger logger,
        IScheduler scheduler,
        ResponseParserOptions parserOptions,
        IAboutFundPageInteractor pageInteractor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pageInteractor = pageInteractor ?? throw new ArgumentNullException(nameof(pageInteractor));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        ArgumentNullException.ThrowIfNull(parserOptions);

        _patterns = parserOptions.Patterns;
    }

    /// <inheritdoc />
    public AboutFundCollectionProgress BeginCollection(AboutFundCollectionSchedule schedule)
    {
        // Cancel all scheduled interactions from the previous fund
        _scheduledInteractions?.Dispose();

        AboutFundPageData? previousData = null;

        lock (_lock)
        {
            // Force-complete previous collection so the parent can decide what to do with partial data
            if (_current != null && _phase != CollectionPhase.Completed)
            {
                _logger.Info("Force-completing collection for {0} (new fund: {1})",
                    _current.OrderBookId, schedule.OrderBookId);
                _phase = CollectionPhase.Completed;
                previousData = _current;
            }

            _current = new AboutFundPageData
            {
                OrderBookId = schedule.OrderBookId
            };
            _phase = CollectionPhase.Interacting;
        }

        // Emit outside lock — parent decides whether to use partial data
        if (previousData != null)
            _completed.OnNext(previousData);

        _logger.Debug("Begin collection for OrderBookId={0}", schedule.OrderBookId);

        var result = ScheduleInteractions(schedule);
        EmitProgress();
        return result;
    }

    /// <inheritdoc />
    public void NotifyResponseCaptured(AboutFundInterceptedRequest request)
    {
        _logger.Trace("Response captured: {0} {1} -> {2}", request.Method, request.Url, request.StatusCode);

        if (!TryRouteResponse(request))
            return;

        bool shouldComplete;
        lock (_lock)
        {
            shouldComplete = _phase == CollectionPhase.Draining;
        }

        if (shouldComplete)
            CompleteCollection();
    }

    #region Response routing

    /// <summary>
    /// Matches an intercepted request URL against configured <see cref="EndpointPattern"/>s
    /// and routes the response data to the appropriate slot. First match wins.
    /// </summary>
    /// <returns><c>true</c> if the URL matched a known pattern; <c>false</c> otherwise.</returns>
    private bool TryRouteResponse(AboutFundInterceptedRequest request)
    {
        var url = request.Url.AbsoluteUri;

        foreach (var endpoint in _patterns)
        {
            if (!endpoint.UrlFragments.All(f => url.Contains(f, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (request.StatusCode is < 200 or >= 300)
            {
                _logger.Warn("Matched {0} but status {1} — marking slot failed",
                    endpoint.Slot, request.StatusCode);
                FailSlot(endpoint.Slot, $"HTTP {request.StatusCode}: {request.StatusText}");
                return true;
            }

            _logger.Debug("Matched {0} ({1} chars)", endpoint.Slot, request.ResponseBody.Length);
            SucceedSlot(endpoint.Slot, request.ResponseBody);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Marks a named slot as succeeded with captured response data.
    /// </summary>
    private void SucceedSlot(AboutFundDataSlot slot, string data)
    {
        UpdateSlot(slot, pd => slot switch
        {
            AboutFundDataSlot.Chart1Month => pd with { Chart1Month = AboutFundFetchSlot.Succeeded(data) },
            AboutFundDataSlot.Chart3Months => pd with { Chart3Months = AboutFundFetchSlot.Succeeded(data) },
            AboutFundDataSlot.ChartYearToDate => pd with { ChartYearToDate = AboutFundFetchSlot.Succeeded(data) },
            AboutFundDataSlot.Chart1Year => pd with { Chart1Year = AboutFundFetchSlot.Succeeded(data) },
            AboutFundDataSlot.Chart3Years => pd with { Chart3Years = AboutFundFetchSlot.Succeeded(data) },
            AboutFundDataSlot.Chart5Years => pd with { Chart5Years = AboutFundFetchSlot.Succeeded(data) },
            AboutFundDataSlot.ChartMax => pd with { ChartMax = AboutFundFetchSlot.Succeeded(data) },
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        });
    }

    /// <summary>
    /// Marks a named slot as failed, recording the reason.
    /// </summary>
    private void FailSlot(AboutFundDataSlot slot, string reason)
    {
        UpdateSlot(slot, pd => slot switch
        {
            AboutFundDataSlot.Chart1Month => pd with { Chart1Month = AboutFundFetchSlot.Failed(reason) },
            AboutFundDataSlot.Chart3Months => pd with { Chart3Months = AboutFundFetchSlot.Failed(reason) },
            AboutFundDataSlot.ChartYearToDate => pd with { ChartYearToDate = AboutFundFetchSlot.Failed(reason) },
            AboutFundDataSlot.Chart1Year => pd with { Chart1Year = AboutFundFetchSlot.Failed(reason) },
            AboutFundDataSlot.Chart3Years => pd with { Chart3Years = AboutFundFetchSlot.Failed(reason) },
            AboutFundDataSlot.Chart5Years => pd with { Chart5Years = AboutFundFetchSlot.Failed(reason) },
            AboutFundDataSlot.ChartMax => pd with { ChartMax = AboutFundFetchSlot.Failed(reason) },
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        });
    }

    /// <summary>
    /// Updates a slot on the current collection and emits progress.
    /// </summary>
    private void UpdateSlot(AboutFundDataSlot slot, Func<AboutFundPageData, AboutFundPageData> update)
    {
        lock (_lock)
        {
            if (_current == null)
            {
                _logger.Warn("Received data for slot '{0}' but no collection is active", slot);
                return;
            }

            _current = update(_current);
        }

        // Emit progress immediately so UI sees slot changes without waiting for the 1s tick
        EmitProgress();
    }

    #endregion

    #region Interaction scheduling

    /// <summary>
    /// Derives relative delays from the schedule's absolute fire times and schedules
    /// interaction timers. Each timer subscription is tracked for cancellation.
    /// Also starts a 1-second progress ticker and a safety-net completion timer.
    /// </summary>
    private AboutFundCollectionProgress ScheduleInteractions(AboutFundCollectionSchedule schedule)
    {
        var disposables = new CompositeDisposable();

        // Convert absolute fire times to relative delays and schedule timers
        foreach (var step in schedule.Steps)
        {
            var delay = step.FireAt - schedule.StartTime;
            var action = ResolveAction(step.Kind);
            disposables.Add(ScheduleStep(delay, step.Kind, action));
        }

        // Safety-net timer — forces completion if Max response never arrives
        disposables.Add(Observable.Timer(schedule.TotalDuration, _scheduler)
            .Subscribe(_ => CompleteCollection()));

        // Build runtime step trackers from scheduled steps
        lock (_lock)
        {
            _steps.Clear();
            foreach (var step in schedule.Steps)
                _steps[step.Kind] = new AboutFundCollectionStep(step.Kind, step.FireAt - schedule.StartTime);
            _collectionStartedAt = schedule.StartTime;
            _totalDuration = schedule.TotalDuration;
        }

        // 1-second progress ticker
        disposables.Add(Observable.Interval(TimeSpan.FromSeconds(1), _scheduler)
            .Subscribe(_ => EmitProgress()));

        _scheduledInteractions = disposables;

        _logger.Info("Scheduled {0} interactions over {1:F0}s",
            schedule.Steps.Count, schedule.TotalDuration.TotalSeconds);

        return BuildProgressSnapshot();
    }

    /// <summary>
    /// Resolves the page interaction action for a given step kind.
    /// </summary>
    private Func<Task<bool>> ResolveAction(AboutFundCollectionStepKind kind)
    {
        return kind switch
        {
            AboutFundCollectionStepKind.ActivateSekView => _pageInteractor.ActivateSekViewAsync,
            AboutFundCollectionStepKind.Select1Month => _pageInteractor.SelectPeriod1MonthAsync,
            AboutFundCollectionStepKind.Select3Months => _pageInteractor.SelectPeriod3MonthsAsync,
            AboutFundCollectionStepKind.SelectYearToDate => _pageInteractor.SelectPeriodYearToDateAsync,
            AboutFundCollectionStepKind.Select1Year => _pageInteractor.SelectPeriod1YearAsync,
            AboutFundCollectionStepKind.Select3Years => _pageInteractor.SelectPeriod3YearsAsync,
            AboutFundCollectionStepKind.Select5Years => _pageInteractor.SelectPeriod5YearsAsync,
            AboutFundCollectionStepKind.SelectMax => _pageInteractor.SelectPeriodMaxAsync,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown step kind")
        };
    }

    /// <summary>
    /// Schedules a single-page interaction at the given cumulative delay.
    /// Marks the step as <see cref="AboutFundCollectionStepStatus.Completed"/> when the action
    /// returns <c>true</c> (element clicked), or <see cref="AboutFundCollectionStepStatus.Failed"/>
    /// when it returns <c>false</c> (element not found or error).
    /// When <see cref="AboutFundCollectionStepKind.SelectMax"/> succeeds, transitions to
    /// <see cref="CollectionPhase.Draining"/> to await the final HTTP response.
    /// </summary>
    private IDisposable ScheduleStep(TimeSpan delay,
        AboutFundCollectionStepKind kind, Func<Task<bool>> action)
    {
        return Observable.Timer(delay, _scheduler)
            .SelectMany(_ => Observable.FromAsync(action))
            .Subscribe(
                clicked =>
                {
                    _logger.Debug("Scheduled step at +{0:F0}s: {1} -> {2}",
                        delay.TotalSeconds, kind, clicked ? "clicked" : "failed");
                    MarkStep(kind, clicked
                        ? AboutFundCollectionStepStatus.Completed
                        : AboutFundCollectionStepStatus.Failed);

                    // Last interaction step — transition to draining to await final response
                    if (clicked && kind == AboutFundCollectionStepKind.SelectMax)
                        lock (_lock)
                        {
                            _phase = CollectionPhase.Draining;
                        }
                },
                ex => _logger.Warn(ex, "Unexpected error in step '{0}'", kind));
    }

    /// <summary>
    /// Updates the status of a step identified by its <see cref="AboutFundCollectionStepKind"/>.
    /// </summary>
    private void MarkStep(AboutFundCollectionStepKind kind, AboutFundCollectionStepStatus status)
    {
        lock (_lock)
        {
            if (_steps.TryGetValue(kind, out var step))
                _steps[kind] = step with { Status = status };
        }
    }

    #endregion

    #region Progress reporting

    /// <summary>
    /// Builds progress snapshot from the current state and emits on <see cref="StateChanged"/>.
    /// </summary>
    private void EmitProgress()
    {
        var snapshot = BuildProgressSnapshot();
        _stateChanged.OnNext(snapshot);
    }

    /// <summary>
    /// Builds an <see cref="AboutFundCollectionProgress"/> snapshot from current state.
    /// </summary>
    private AboutFundCollectionProgress BuildProgressSnapshot()
    {
        lock (_lock)
        {
            var elapsed = _scheduler.Now - _collectionStartedAt;
            var remaining = _totalDuration - elapsed;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            return new AboutFundCollectionProgress
            {
                OrderBookId = _current?.OrderBookId ?? default,
                Steps = [.. _steps.Values],
                TotalDuration = _totalDuration,
                Elapsed = elapsed,
                Remaining = remaining,
                PageData = _current ?? new AboutFundPageData { OrderBookId = default }
            };
        }
    }

    #endregion

    /// <summary>
    /// Marks the current collection as complete and emits on <see cref="Completed"/>.
    /// </summary>
    private void CompleteCollection()
    {
        AboutFundPageData snapshot;

        lock (_lock)
        {
            if (_current == null || _phase == CollectionPhase.Completed) return;

            _phase = CollectionPhase.Completed;
            snapshot = _current;
        }

        _logger.Info("Collection complete for {0}: {1}",
            snapshot.OrderBookId,
            snapshot.IsFullySuccessful ? "all succeeded" : "partial");
        _completed.OnNext(snapshot);
    }

    /// <inheritdoc />
    public void CancelCollection()
    {
        _scheduledInteractions?.Dispose();
        _scheduledInteractions = null;

        lock (_lock)
        {
            _current = null;
            _phase = CollectionPhase.Idle;
            _steps.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _scheduledInteractions?.Dispose();
        _completed.Dispose();
        _stateChanged.Dispose();
        _disposed = true;
    }
}