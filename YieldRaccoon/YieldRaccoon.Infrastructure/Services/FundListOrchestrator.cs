using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events.FundList;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Orchestrates fund list session lifecycle, batch loading workflow, and timer management.
/// </summary>
/// <remarks>
/// <para>
/// This service owns all session-related business logic:
/// <list type="bullet">
///   <item>Session lifecycle (start, cancel, complete)</item>
///   <item>Pre-calculated batch schedule with upfront timer scheduling</item>
///   <item>In-memory state tracking with phase-based state machine</item>
///   <item>Domain event publishing to event store (for auditing)</item>
///   <item>State projection from in-memory fields to observable streams</item>
/// </list>
/// </para>
/// <para>
/// <strong>Phase lifecycle:</strong>
/// <c>Idle → DelayBeforeNextBatch → Loading → DelayBeforeNextBatch → … → Idle</c>.
/// </para>
/// </remarks>
public class FundListOrchestrator : IFundListOrchestrator
{
    private readonly ILogger _logger;
    private readonly IFundListEventStore _eventStore;
    private readonly IFundListScheduleCalculator _scheduleCalculator;
    private readonly IFundListIngestionService _fundIngestionService;
    private readonly IScheduler _scheduler;
    private readonly CompositeDisposable _disposables = new();

    #region Session state

    /// <summary>Unique correlation ID for the active session.</summary>
    private FundListSessionId? _currentSessionId;

    /// <summary>Pre-calculated timing for every batch in the session (ordered).</summary>
    private List<FundListBatchSchedule> _batchSchedules = [];

    /// <summary>Per-batch status — authoritative state for skip/advance logic.</summary>
    private readonly Dictionary<FundListBatchNumber, FundListBatchStatus> _batchStatuses = new();

    /// <summary>Per-batch fund counts — tracks how many funds were loaded in each batch.</summary>
    private readonly Dictionary<FundListBatchNumber, int> _fundsPerBatch = new();

    /// <summary>Explicit lifecycle phase — replaces implicit derivation from event store.</summary>
    private FundListSessionPhase _phase;

    /// <summary>Identity of the batch currently being loaded (or about to be loaded after delay).</summary>
    private FundListBatchNumber? _currentBatchNumber;

    /// <summary>Total funds loaded across all completed batches in this session.</summary>
    private int _totalFundsLoaded;

    /// <summary>Timestamp when the session started.</summary>
    private DateTimeOffset _sessionStartedAt;

    /// <summary>Controls whether remaining batches are rescheduled after a batch completes.</summary>
    private bool _autoAdvanceEnabled;

    /// <summary>
    /// All scheduled batch timers + 1s ticker.
    /// Disposed on cancel, advance, or session end.
    /// </summary>
    private CompositeDisposable? _scheduledBatchTimers;

    private bool _disposed;

    #endregion

    // BehaviorSubjects for state (emit current value to new subscribers)
    private readonly BehaviorSubject<FundListSessionState> _sessionState;
    private readonly BehaviorSubject<IReadOnlyList<FundListScheduledBatchItem>> _scheduledBatches;

    // Subjects for events (no initial value)
    private readonly Subject<FundListCountdownTick> _countdownTick = new();
    private readonly Subject<FundListBatchNumber> _loadBatchRequested = new();
    private readonly Subject<FundListSessionCompletedInfo> _sessionCompleted = new();

    /// <inheritdoc/>
    public IObservable<FundListSessionState> SessionState => _sessionState.AsObservable();

    /// <inheritdoc/>
    public IObservable<IReadOnlyList<FundListScheduledBatchItem>> ScheduledBatches => _scheduledBatches.AsObservable();

    /// <inheritdoc/>
    public IObservable<FundListCountdownTick> CountdownTick => _countdownTick.AsObservable();

    /// <inheritdoc/>
    public IObservable<FundListBatchNumber> LoadBatchRequested => _loadBatchRequested.AsObservable();

    /// <inheritdoc/>
    public IObservable<FundListSessionCompletedInfo> SessionCompleted => _sessionCompleted.AsObservable();

    /// <summary>
    /// Initializes a new instance of the <see cref="FundListOrchestrator"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="eventStore">Event store for fund list session events.</param>
    /// <param name="scheduleCalculator">Pure computation service for pre-calculating batch timings.</param>
    /// <param name="fundIngestionService">Service for persisting fund data to the database.</param>
    /// <param name="scheduler">Rx scheduler for timer operations.</param>
    public FundListOrchestrator(
        ILogger logger,
        IFundListEventStore eventStore,
        IFundListScheduleCalculator scheduleCalculator,
        IFundListIngestionService fundIngestionService,
        IScheduler scheduler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _scheduleCalculator = scheduleCalculator ?? throw new ArgumentNullException(nameof(scheduleCalculator));
        _fundIngestionService = fundIngestionService ?? throw new ArgumentNullException(nameof(fundIngestionService));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

        _phase = FundListSessionPhase.Idle;

        // Initialize with inactive state
        _sessionState = new BehaviorSubject<FundListSessionState>(FundListSessionState.Inactive);
        _scheduledBatches = new BehaviorSubject<IReadOnlyList<FundListScheduledBatchItem>>(Array.Empty<FundListScheduledBatchItem>());

        _logger.Debug("FundListOrchestrator initialized");
    }

    /// <inheritdoc/>
    public FundListSessionId StartSession()
    {
        _logger.Info("Starting new fund list session");

        _currentSessionId = FundListSessionId.NewId();
        _sessionStartedAt = _scheduler.Now.UtcDateTime;
        _totalFundsLoaded = 0;

        // Pre-calculate all batch timings
        var expectedBatchCount = FundListScheduleCalculator.DefaultExpectedBatchCount;
        _batchSchedules = _scheduleCalculator.CalculateSessionSchedule(
            expectedBatchCount,
            _scheduler.Now + TimeSpan.FromSeconds(15));

        _logger.Info("Session {0} scheduled with {1} batches", _currentSessionId, expectedBatchCount);

        // Initialize all batches as Pending
        _batchStatuses.Clear();
        _fundsPerBatch.Clear();
        foreach (var schedule in _batchSchedules)
            _batchStatuses[schedule.BatchNumber] = FundListBatchStatus.Pending;

        // Append events for auditing
        var totalSeconds = _batchSchedules.Count > 0
            ? (int)(_batchSchedules[^1].ScheduledAt - _batchSchedules[0].ScheduledAt).TotalSeconds
            : 0;

        _eventStore.Append(FundListSessionStarted.Create(
            _currentSessionId.Value,
            0, // Unknown upfront
            0, // Unknown upfront
            expectedBatchCount,
            totalSeconds));

        foreach (var schedule in _batchSchedules)
        {
            var cumulativeDelay = (int)(schedule.ScheduledAt - _batchSchedules[0].ScheduledAt + schedule.DelayBeforeBatch).TotalSeconds;
            _eventStore.Append(FundListBatchScheduled.Create(
                _currentSessionId.Value,
                schedule.BatchNumber,
                cumulativeDelay));
        }

        // Set initial state — first batch is about to load after delay
        _phase = FundListSessionPhase.DelayBeforeNextBatch;
        _currentBatchNumber = new FundListBatchNumber(1);

        // Schedule ALL batch timers upfront
        ScheduleBatchTimers(new FundListBatchNumber(1));

        RefreshState();
        RefreshScheduledBatches();

        return _currentSessionId.Value;
    }

    /// <inheritdoc/>
    public void AdvanceToNextBatch()
    {
        if (_currentSessionId == null)
        {
            _logger.Warn("AdvanceToNextBatch called but no session is active");
            return;
        }

        CancelScheduledBatchTimers();

        var nextBatch = GetNextPendingBatch();
        if (nextBatch == null)
        {
            CompleteSession();
            return;
        }

        // Skip delay — immediately load the next batch
        ExecuteBatchLoad(nextBatch);

        // Reschedule remaining batches when auto-advance is enabled
        if (_autoAdvanceEnabled)
        {
            var afterNext = GetNextPendingBatchAfter(nextBatch.BatchNumber);
            if (afterNext != null)
            {
                _batchSchedules = _scheduleCalculator.RecalculateRemainingSchedule(
                    _batchSchedules, afterNext.BatchNumber, _scheduler.Now, _batchStatuses);
                ScheduleBatchTimers(afterNext.BatchNumber);
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
    public void CancelSession(string reason)
    {
        if (_currentSessionId == null)
        {
            _logger.Warn("CancelSession called but no session is active");
            return;
        }

        _logger.Info("Cancelling session {0}: {1}", _currentSessionId, reason);

        CancelScheduledBatchTimers();

        var sessionId = _currentSessionId.Value;

        _eventStore.Append(FundListSessionCancelled.Create(
            sessionId,
            _totalFundsLoaded,
            reason,
            _sessionStartedAt));

        _logger.Info("Session {0} cancelled after loading {1} funds", sessionId, _totalFundsLoaded);

        ResetSessionState();
        RefreshState();
        RefreshScheduledBatches();
    }

    /// <inheritdoc/>
    public async Task NotifyBatchLoadedAsync(IReadOnlyCollection<FundListDataDto> funds, int totalFundsLoaded, bool hasMore)
    {
        if (_currentSessionId == null || _phase == FundListSessionPhase.Idle)
        {
            _logger.Trace("NotifyBatchLoadedAsync called but no active session");
            return;
        }

        var sessionId = _currentSessionId.Value;
        var completedBatch = _currentBatchNumber ?? new FundListBatchNumber(1);
        var fundsInBatch = funds.Count;

        _logger.Info("Batch {0} completed for session {1}: {2} funds (total: {3})",
            completedBatch.Value, sessionId, fundsInBatch, totalFundsLoaded);

        // Persist fund data to database via ingestion service
        var ingestedCount = await _fundIngestionService.IngestBatchAsync(funds);
        _logger.Info("Persisted {0}/{1} funds to database", ingestedCount, fundsInBatch);

        // Update in-memory tracking
        _batchStatuses[completedBatch] = FundListBatchStatus.Completed;
        _fundsPerBatch[completedBatch] = fundsInBatch;
        _totalFundsLoaded = totalFundsLoaded;

        // Append batch completed event for auditing
        _eventStore.Append(FundListBatchCompleted.Create(
            sessionId,
            completedBatch,
            fundsInBatch,
            totalFundsLoaded));

        if (hasMore)
        {
            // Find next pending batch — its timer is already scheduled
            var nextBatch = GetNextPendingBatchAfter(completedBatch);
            if (nextBatch != null)
            {
                _logger.Debug("Next batch {0} scheduled at {1}",
                    nextBatch.BatchNumber.Value, nextBatch.ScheduledAt);

                _phase = FundListSessionPhase.DelayBeforeNextBatch;
                _currentBatchNumber = nextBatch.BatchNumber;

                // Append delay started event for auditing
                var delaySeconds = Math.Max(1, (int)(nextBatch.ScheduledAt - _scheduler.Now).TotalSeconds);
                _eventStore.Append(FundListBatchDelayStarted.Create(
                    sessionId,
                    nextBatch.BatchNumber,
                    delaySeconds));
            }
            else
            {
                _logger.Warn("No more scheduled batches but HasMore=true - ending session");
                CompleteSession();
                return;
            }
        }
        else
        {
            // Session complete - no more funds
            CompleteSession();
            return;
        }

        RefreshState();
        RefreshScheduledBatches();
    }

    /// <inheritdoc/>
    public async Task<int> IngestFundsAsync(IReadOnlyCollection<FundListDataDto> funds)
    {
        if (funds == null || funds.Count == 0)
        {
            _logger.Trace("IngestFundsAsync called with empty collection");
            return 0;
        }

        _logger.Info("Ingesting {0} funds (no active session)", funds.Count);

        try
        {
            var ingestedCount = await _fundIngestionService.IngestBatchAsync(funds);
            _logger.Info("Successfully persisted {0}/{1} funds to database", ingestedCount, funds.Count);
            return ingestedCount;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to ingest funds to database");
            return 0;
        }
    }

    #region Timer scheduling

    /// <summary>
    /// Schedules <see cref="Observable.Timer(TimeSpan,IScheduler)"/> for each pending batch starting from
    /// <paramref name="fromBatch"/> at the pre-calculated fire times.
    /// Includes a 1-second ticker for UI countdown refresh.
    /// </summary>
    private void ScheduleBatchTimers(FundListBatchNumber fromBatch)
    {
        CancelScheduledBatchTimers();

        var disposables = new CompositeDisposable();
        var now = _scheduler.Now;

        foreach (var entry in _batchSchedules)
        {
            if (entry.BatchNumber < fromBatch) continue;

            // Skip already completed or in-progress batches
            if (_batchStatuses.TryGetValue(entry.BatchNumber, out var status)
                && status != FundListBatchStatus.Pending)
                continue;

            var delay = entry.ScheduledAt - now;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            var captured = entry;
            disposables.Add(Observable.Timer(delay, _scheduler)
                .Subscribe(_ => ExecuteBatchLoad(captured)));
        }

        // 1-second ticker for delay countdown and progress display
        disposables.Add(Observable.Interval(TimeSpan.FromSeconds(1), _scheduler)
            .Subscribe(_ => RefreshState()));

        _scheduledBatchTimers = disposables;
    }

    /// <summary>
    /// Immediately loads a batch by transitioning to <see cref="FundListSessionPhase.Loading"/>
    /// and emitting a <see cref="LoadBatchRequested"/> intent signal.
    /// </summary>
    private void ExecuteBatchLoad(FundListBatchSchedule batchSchedule)
    {
        if (_currentSessionId == null) return;

        // Guard: skip if batch already loaded (e.g., from advance + timer race)
        if (_batchStatuses.TryGetValue(batchSchedule.BatchNumber, out var status)
            && status != FundListBatchStatus.Pending)
            return;

        _phase = FundListSessionPhase.Loading;
        _currentBatchNumber = batchSchedule.BatchNumber;
        _batchStatuses[batchSchedule.BatchNumber] = FundListBatchStatus.InProgress;

        _logger.Debug("Loading batch {0}", batchSchedule.BatchNumber.Value);

        // Append events for auditing
        _eventStore.Append(FundListBatchDelayCompleted.Create(
            _currentSessionId.Value,
            batchSchedule.BatchNumber));

        _eventStore.Append(FundListBatchStarted.Create(
            _currentSessionId.Value,
            batchSchedule.BatchNumber));

        RefreshState();
        RefreshScheduledBatches();

        // Request the view to load the batch (Intent Signal Pattern)
        _loadBatchRequested.OnNext(batchSchedule.BatchNumber);
    }

    /// <summary>
    /// Cancels all pending batch timers and the progress ticker.
    /// </summary>
    private void CancelScheduledBatchTimers()
    {
        _scheduledBatchTimers?.Dispose();
        _scheduledBatchTimers = null;
    }

    #endregion

    #region Session lifecycle

    /// <summary>
    /// Completes the current session and emits completion information.
    /// </summary>
    private void CompleteSession()
    {
        if (_currentSessionId == null) return;

        CancelScheduledBatchTimers();

        var sessionId = _currentSessionId.Value;
        var totalBatchesLoaded = _batchStatuses.Count(kv => kv.Value == FundListBatchStatus.Completed);

        _logger.Info("Session {0} complete - all funds loaded", sessionId);

        var timestamps = _eventStore.GetBatchLoadTimestamps(sessionId);

        _eventStore.Append(FundListSessionCompleted.Create(
            sessionId,
            _totalFundsLoaded,
            totalBatchesLoaded,
            _sessionStartedAt,
            timestamps.ToList()));

        var duration = _scheduler.Now - _sessionStartedAt;

        // Emit completion info
        _sessionCompleted.OnNext(new FundListSessionCompletedInfo
        {
            SessionId = sessionId,
            TotalFundsLoaded = _totalFundsLoaded,
            TotalBatches = totalBatchesLoaded,
            Duration = duration
        });

        // Emit final state before resetting
        _sessionState.OnNext(new FundListSessionState
        {
            Phase = FundListSessionPhase.Idle,
            IsActive = false,
            SessionId = sessionId,
            CurrentBatchNumber = totalBatchesLoaded,
            EstimatedBatchCount = _batchSchedules.Count,
            FundsLoaded = _totalFundsLoaded,
            EstimatedTimeRemaining = TimeSpan.Zero,
            IsDelayInProgress = false,
            StatusMessage = $"Complete! Loaded {_totalFundsLoaded} funds in {duration:mm\\:ss}",
            DelayCountdown = 0
        });

        ResetSessionState();
        RefreshScheduledBatches();
    }

    /// <summary>
    /// Resets all in-memory session state to idle.
    /// </summary>
    private void ResetSessionState()
    {
        _currentSessionId = null;
        _currentBatchNumber = null;
        _totalFundsLoaded = 0;
        _batchSchedules = [];
        _batchStatuses.Clear();
        _fundsPerBatch.Clear();
        _phase = FundListSessionPhase.Idle;
    }

    #endregion

    #region State projection

    /// <summary>
    /// Refreshes session state and emits update. Also emits countdown ticks during delay phase.
    /// </summary>
    private void RefreshState()
    {
        var state = ProjectState();
        _sessionState.OnNext(state);

        // Emit countdown tick for backward compat during delay phase
        if (_phase == FundListSessionPhase.DelayBeforeNextBatch && _currentBatchNumber.HasValue)
        {
            var schedule = GetBatchSchedule(_currentBatchNumber.Value);
            if (schedule != null)
            {
                var remaining = schedule.ScheduledAt - _scheduler.Now;
                var seconds = Math.Max(0, (int)remaining.TotalSeconds);
                _countdownTick.OnNext(new FundListCountdownTick(
                    _currentBatchNumber.Value, seconds, schedule.DelayBeforeBatch));
            }
        }
    }

    /// <summary>
    /// Projects current session state from in-memory tracking fields.
    /// </summary>
    private FundListSessionState ProjectState()
    {
        if (_currentSessionId == null || _phase == FundListSessionPhase.Idle)
            return FundListSessionState.Inactive;

        var completedBatches = _batchStatuses.Count(kv => kv.Value == FundListBatchStatus.Completed);

        // Delay countdown from pre-calculated schedule
        var delayCountdown = 0;
        if (_phase == FundListSessionPhase.DelayBeforeNextBatch && _currentBatchNumber.HasValue)
        {
            var schedule = GetBatchSchedule(_currentBatchNumber.Value);
            if (schedule != null)
            {
                var remaining = schedule.ScheduledAt - _scheduler.Now;
                delayCountdown = Math.Max(0, (int)remaining.TotalSeconds);
            }
        }

        // ETA from pre-calculated schedule — time until last pending batch fires
        var lastPending = _batchSchedules
            .LastOrDefault(s => _batchStatuses.TryGetValue(s.BatchNumber, out var st)
                                && st == FundListBatchStatus.Pending);

        var estimatedTimeRemaining = lastPending != null
            ? lastPending.ScheduledAt - _scheduler.Now
            : TimeSpan.Zero;

        if (estimatedTimeRemaining < TimeSpan.Zero)
            estimatedTimeRemaining = TimeSpan.Zero;

        var statusMessage = _phase == FundListSessionPhase.DelayBeforeNextBatch
            ? $"Next batch in {delayCountdown}s..."
            : $"Loading batch {_currentBatchNumber?.Value ?? 0}...";

        return new FundListSessionState
        {
            Phase = _phase,
            IsActive = true,
            SessionId = _currentSessionId.Value,
            CurrentBatchNumber = completedBatches,
            EstimatedBatchCount = _batchSchedules.Count,
            FundsLoaded = _totalFundsLoaded,
            EstimatedTimeRemaining = estimatedTimeRemaining,
            IsDelayInProgress = _phase == FundListSessionPhase.DelayBeforeNextBatch,
            StatusMessage = statusMessage,
            DelayCountdown = delayCountdown
        };
    }

    /// <summary>
    /// Refreshes the scheduled batches list from in-memory state.
    /// </summary>
    private void RefreshScheduledBatches()
    {
        if (_currentSessionId == null)
        {
            _scheduledBatches.OnNext(Array.Empty<FundListScheduledBatchItem>());
            return;
        }

        var items = _batchSchedules.Select(s => new FundListScheduledBatchItem
        {
            BatchNumber = s.BatchNumber,
            ScheduledAt = s.ScheduledAt,
            Status = _batchStatuses.TryGetValue(s.BatchNumber, out var st)
                ? st
                : FundListBatchStatus.Pending,
            FundsLoaded = _fundsPerBatch.TryGetValue(s.BatchNumber, out var funds) ? funds : null
        }).ToList();

        _scheduledBatches.OnNext(items);
    }

    #endregion

    #region Schedule helpers

    /// <summary>
    /// Returns the <see cref="FundListBatchSchedule"/> for the given batch number, or null.
    /// </summary>
    private FundListBatchSchedule? GetBatchSchedule(FundListBatchNumber batchNumber)
    {
        foreach (var schedule in _batchSchedules)
        {
            if (schedule.BatchNumber == batchNumber)
                return schedule;
        }

        return null;
    }

    /// <summary>
    /// Returns the next pending batch (any batch with Pending status), or null.
    /// </summary>
    private FundListBatchSchedule? GetNextPendingBatch()
    {
        foreach (var schedule in _batchSchedules)
        {
            if (_batchStatuses.TryGetValue(schedule.BatchNumber, out var status)
                && status == FundListBatchStatus.Pending)
                return schedule;
        }

        return null;
    }

    /// <summary>
    /// Returns the next <see cref="FundListBatchSchedule"/> after the given batch number
    /// that has <see cref="FundListBatchStatus.Pending"/> status, or null if none remain.
    /// </summary>
    private FundListBatchSchedule? GetNextPendingBatchAfter(FundListBatchNumber afterBatch)
    {
        var found = false;
        foreach (var schedule in _batchSchedules)
        {
            if (schedule.BatchNumber == afterBatch)
            {
                found = true;
                continue;
            }

            if (found && _batchStatuses.TryGetValue(schedule.BatchNumber, out var status)
                       && status == FundListBatchStatus.Pending)
                return schedule;
        }

        return null;
    }

    #endregion

    /// <summary>
    /// Releases all resources used by the orchestrator.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.Debug("FundListOrchestrator disposing");

        CancelScheduledBatchTimers();
        _disposables.Dispose();
        _sessionState.Dispose();
        _scheduledBatches.Dispose();
        _countdownTick.Dispose();
        _loadBatchRequested.Dispose();
        _sessionCompleted.Dispose();

        _disposed = true;
    }
}
