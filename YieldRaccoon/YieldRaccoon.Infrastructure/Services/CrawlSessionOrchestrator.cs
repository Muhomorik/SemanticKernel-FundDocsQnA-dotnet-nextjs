using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Orchestrates crawl session lifecycle, batch loading workflow, and timer management.
/// </summary>
/// <remarks>
/// <para>
/// This service owns all session-related business logic that was previously in the ViewModel:
/// <list type="bullet">
///   <item>Session lifecycle (start, cancel, complete)</item>
///   <item>Batch workflow orchestration</item>
///   <item>Countdown timer management using Rx.NET</item>
///   <item>Domain event publishing to event store</item>
///   <item>State projection from events to observable streams</item>
/// </list>
/// </para>
/// </remarks>
public class CrawlSessionOrchestrator : ICrawlSessionOrchestrator
{
    private readonly ILogger _logger;
    private readonly ICrawlEventStore _eventStore;
    private readonly ICrawlSessionScheduler _sessionScheduler;
    private readonly IFundIngestionService _fundIngestionService;
    private readonly IScheduler _scheduler;
    private readonly CompositeDisposable _disposables = new();

    // Current session tracking
    private CrawlSessionId? _currentSessionId;
    private IDisposable? _countdownSubscription;
    private bool _disposed;

    // BehaviorSubjects for state (emit current value to new subscribers)
    private readonly BehaviorSubject<CrawlSessionState> _sessionState;
    private readonly BehaviorSubject<IReadOnlyList<ScheduledBatchItem>> _scheduledBatches;

    // Subjects for events (no initial value)
    private readonly Subject<CountdownTick> _countdownTick = new();
    private readonly Subject<BatchNumber> _loadBatchRequested = new();
    private readonly Subject<SessionCompletedInfo> _sessionCompleted = new();

    /// <inheritdoc/>
    public IObservable<CrawlSessionState> SessionState => _sessionState.AsObservable();

    /// <inheritdoc/>
    public IObservable<IReadOnlyList<ScheduledBatchItem>> ScheduledBatches => _scheduledBatches.AsObservable();

    /// <inheritdoc/>
    public IObservable<CountdownTick> CountdownTick => _countdownTick.AsObservable();

    /// <inheritdoc/>
    public IObservable<BatchNumber> LoadBatchRequested => _loadBatchRequested.AsObservable();

    /// <inheritdoc/>
    public IObservable<SessionCompletedInfo> SessionCompleted => _sessionCompleted.AsObservable();

    /// <summary>
    /// Initializes a new instance of the <see cref="CrawlSessionOrchestrator"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="eventStore">Event store for crawl session events.</param>
    /// <param name="sessionScheduler">Scheduler for pre-calculating batch timings.</param>
    /// <param name="fundIngestionService">Service for persisting fund data to the database.</param>
    /// <param name="scheduler">Rx scheduler for timer operations.</param>
    public CrawlSessionOrchestrator(
        ILogger logger,
        ICrawlEventStore eventStore,
        ICrawlSessionScheduler sessionScheduler,
        IFundIngestionService fundIngestionService,
        IScheduler scheduler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _sessionScheduler = sessionScheduler ?? throw new ArgumentNullException(nameof(sessionScheduler));
        _fundIngestionService = fundIngestionService ?? throw new ArgumentNullException(nameof(fundIngestionService));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

        // Initialize with inactive state
        _sessionState = new BehaviorSubject<CrawlSessionState>(CrawlSessionState.Inactive);
        _scheduledBatches = new BehaviorSubject<IReadOnlyList<ScheduledBatchItem>>(Array.Empty<ScheduledBatchItem>());

        _logger.Debug("CrawlSessionOrchestrator initialized");
    }

    /// <inheritdoc/>
    public CrawlSessionId StartSession()
    {
        _logger.Info("Starting new crawl session");

        // Scheduler pre-calculates all batch times and appends events
        _currentSessionId = _sessionScheduler.ScheduleSession(CrawlSessionScheduler.DefaultExpectedBatchCount);
        _logger.Info("Session {0} scheduled with {1} batches", _currentSessionId, CrawlSessionScheduler.DefaultExpectedBatchCount);

        RefreshStateFromEventStore();
        RefreshScheduledBatches();

        // Start first batch immediately
        var firstBatch = new BatchNumber(1);
        _eventStore.Append(BatchLoadStarted.Create(_currentSessionId.Value, firstBatch));

        // Emit state update with status message
        EmitStateUpdate("Session started - loading first batch...");

        _loadBatchRequested.OnNext(firstBatch);

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

        // Cancel any active countdown timer
        _countdownSubscription?.Dispose();
        _countdownSubscription = null;

        var sessionId = _currentSessionId.Value;
        var fundsLoaded = _eventStore.GetTotalFundsLoaded(sessionId);
        var activeSession = _eventStore.GetActiveSession();
        var startedAt = activeSession?.OccurredAt ?? DateTimeOffset.UtcNow;

        _eventStore.Append(CrawlSessionCancelled.Create(
            sessionId,
            fundsLoaded,
            reason,
            startedAt));

        _logger.Info("Session {0} cancelled after loading {1} funds", sessionId, fundsLoaded);
        _currentSessionId = null;

        RefreshStateFromEventStore();
        RefreshScheduledBatches();
    }

    /// <inheritdoc/>
    public async Task NotifyBatchLoadedAsync(IReadOnlyCollection<FundDataDto> funds, int totalFundsLoaded, bool hasMore)
    {
        if (_currentSessionId == null || !_eventStore.IsSessionActive(_currentSessionId.Value))
        {
            _logger.Trace("NotifyBatchLoadedAsync called but no active session");
            return;
        }

        var sessionId = _currentSessionId.Value;
        var completedBatchNumber = _eventStore.GetCompletedBatchCount(sessionId) + 1;
        var fundsInBatch = funds.Count;

        _logger.Info("Batch {0} completed for session {1}: {2} funds (total: {3})",
            completedBatchNumber, sessionId, fundsInBatch, totalFundsLoaded);

        // Persist fund data to database via ingestion service
        var ingestedCount = await _fundIngestionService.IngestBatchAsync(funds);
        _logger.Info("Persisted {0}/{1} funds to database", ingestedCount, fundsInBatch);

        // Append batch completed event
        _eventStore.Append(BatchLoadCompleted.Create(
            sessionId,
            new BatchNumber(completedBatchNumber),
            fundsInBatch,
            totalFundsLoaded));

        RefreshScheduledBatches();

        if (hasMore)
        {
            // Get next scheduled batch (pre-calculated time)
            var nextScheduled = _eventStore.GetNextScheduledBatch(sessionId);
            if (nextScheduled != null)
            {
                _logger.Debug("Next batch {0} scheduled at {1}",
                    nextScheduled.BatchNumber.Value, nextScheduled.ScheduledAt);
                StartCountdown(nextScheduled);
            }
            else
            {
                _logger.Warn("No more scheduled batches but HasMore=true - ending session");
                CompleteSession(completedBatchNumber);
            }
        }
        else
        {
            // Session complete - no more funds
            CompleteSession(completedBatchNumber);
        }

        RefreshStateFromEventStore();
    }

    /// <inheritdoc/>
    public async Task<int> IngestFundsAsync(IReadOnlyCollection<FundDataDto> funds)
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

    /// <summary>
    /// Starts a countdown timer before loading the next batch.
    /// </summary>
    /// <param name="scheduled">The scheduled batch to count down to.</param>
    private void StartCountdown(BatchLoadScheduled scheduled)
    {
        // Cancel any existing countdown
        _countdownSubscription?.Dispose();

        var delaySeconds = Math.Max(1, (int)(scheduled.ScheduledAt - DateTimeOffset.UtcNow).TotalSeconds);
        var totalDelay = TimeSpan.FromSeconds(delaySeconds);

        _logger.Debug("Starting countdown for {0} seconds before batch {1}",
            delaySeconds, scheduled.BatchNumber.Value);

        // Append delay started event
        _eventStore.Append(BatchLoadDelayStarted.Create(
            _currentSessionId!.Value,
            scheduled.BatchNumber,
            delaySeconds));

        RefreshStateFromEventStore();

        _countdownSubscription = Observable
            .Interval(TimeSpan.FromSeconds(1), _scheduler)
            .Take(delaySeconds)
            .Subscribe(
                tick =>
                {
                    var remaining = delaySeconds - (int)tick - 1;
                    _countdownTick.OnNext(new CountdownTick(scheduled.BatchNumber, remaining, totalDelay));

                    // Update state with countdown
                    EmitStateUpdate($"Next batch in {remaining}s...", remaining);
                },
                () =>
                {
                    _logger.Debug("Countdown complete - loading batch {0}", scheduled.BatchNumber.Value);

                    // Append delay completed event
                    _eventStore.Append(BatchLoadDelayCompleted.Create(
                        _currentSessionId!.Value,
                        scheduled.BatchNumber));

                    // Append batch started event
                    _eventStore.Append(BatchLoadStarted.Create(
                        _currentSessionId!.Value,
                        scheduled.BatchNumber));

                    RefreshStateFromEventStore();
                    RefreshScheduledBatches();

                    // Request the view to load the batch
                    _loadBatchRequested.OnNext(scheduled.BatchNumber);
                });

        _disposables.Add(_countdownSubscription);
    }

    /// <summary>
    /// Completes the current session and emits completion information.
    /// </summary>
    /// <param name="totalBatchesLoaded">Total number of batches loaded.</param>
    private void CompleteSession(int totalBatchesLoaded)
    {
        if (_currentSessionId == null) return;

        var sessionId = _currentSessionId.Value;
        _logger.Info("Session {0} complete - all funds loaded", sessionId);

        var activeSession = _eventStore.GetActiveSession();
        var startedAt = activeSession?.OccurredAt ?? DateTimeOffset.UtcNow;
        var timestamps = _eventStore.GetBatchLoadTimestamps(sessionId);
        var fundsLoaded = _eventStore.GetTotalFundsLoaded(sessionId);

        _eventStore.Append(CrawlSessionCompleted.Create(
            sessionId,
            fundsLoaded,
            totalBatchesLoaded,
            startedAt,
            timestamps.ToList()));

        var duration = DateTimeOffset.UtcNow - startedAt;

        // Emit completion info
        _sessionCompleted.OnNext(new SessionCompletedInfo
        {
            SessionId = sessionId,
            TotalFundsLoaded = fundsLoaded,
            TotalBatches = totalBatchesLoaded,
            Duration = duration
        });

        _currentSessionId = null;

        // Emit final state
        EmitStateUpdate($"Complete! Loaded {fundsLoaded} funds in {duration:mm\\:ss}");
        RefreshScheduledBatches();
    }

    /// <summary>
    /// Refreshes session state from the event store and emits update.
    /// </summary>
    private void RefreshStateFromEventStore()
    {
        var state = ProjectStateFromEventStore();
        _sessionState.OnNext(state);
    }

    /// <summary>
    /// Emits a state update with a custom status message.
    /// </summary>
    private void EmitStateUpdate(string statusMessage, int? delayCountdown = null)
    {
        var baseState = ProjectStateFromEventStore();
        var state = baseState with
        {
            StatusMessage = statusMessage,
            DelayCountdown = delayCountdown ?? baseState.DelayCountdown
        };
        _sessionState.OnNext(state);
    }

    /// <summary>
    /// Projects current session state from the event store.
    /// </summary>
    /// <returns>The current session state.</returns>
    private CrawlSessionState ProjectStateFromEventStore()
    {
        var activeSession = _eventStore.GetActiveSession();
        if (activeSession == null || _currentSessionId == null)
            return CrawlSessionState.Inactive;

        var sessionId = _currentSessionId.Value;
        var completedBatches = _eventStore.GetCompletedBatchCount(sessionId);
        var fundsLoaded = _eventStore.GetTotalFundsLoaded(sessionId);
        var pendingBatches = _eventStore.GetPendingBatchLoads(sessionId);

        var estimatedTimeRemaining = pendingBatches.Count > 0
            ? pendingBatches[^1].ScheduledAt - DateTimeOffset.UtcNow
            : TimeSpan.Zero;

        if (estimatedTimeRemaining < TimeSpan.Zero)
            estimatedTimeRemaining = TimeSpan.Zero;

        // Determine if delay is in progress
        var sessionEvents = _eventStore.GetSessionEvents(sessionId);
        var lastDelayStarted = sessionEvents.OfType<BatchLoadDelayStarted>().LastOrDefault();
        var lastDelayCompleted = sessionEvents.OfType<BatchLoadDelayCompleted>().LastOrDefault();
        var isDelayInProgress = lastDelayStarted != null &&
            (lastDelayCompleted == null || lastDelayCompleted.OccurredAt < lastDelayStarted.OccurredAt);

        return new CrawlSessionState
        {
            IsActive = true,
            SessionId = sessionId,
            CurrentBatchNumber = completedBatches,
            EstimatedBatchCount = activeSession.EstimatedBatchCount,
            FundsLoaded = fundsLoaded,
            EstimatedTimeRemaining = estimatedTimeRemaining,
            IsDelayInProgress = isDelayInProgress,
            StatusMessage = isDelayInProgress ? "Waiting for next batch..." : "Loading batch...",
            DelayCountdown = 0
        };
    }

    /// <summary>
    /// Refreshes the scheduled batches list from the event store.
    /// </summary>
    private void RefreshScheduledBatches()
    {
        if (_currentSessionId == null)
        {
            _scheduledBatches.OnNext(Array.Empty<ScheduledBatchItem>());
            return;
        }

        var sessionId = _currentSessionId.Value;
        var sessionEvents = _eventStore.GetSessionEvents(sessionId);

        var scheduled = sessionEvents.OfType<BatchLoadScheduled>().ToList();
        var started = sessionEvents.OfType<BatchLoadStarted>().Select(e => e.BatchNumber).ToHashSet();
        var completed = sessionEvents.OfType<BatchLoadCompleted>().ToDictionary(e => e.BatchNumber, e => e.FundsInBatch);
        var failed = sessionEvents.OfType<BatchLoadFailed>().Select(e => e.BatchNumber).ToHashSet();

        var items = scheduled.Select(s =>
        {
            BatchStatus status;
            if (completed.ContainsKey(s.BatchNumber))
                status = BatchStatus.Completed;
            else if (failed.Contains(s.BatchNumber))
                status = BatchStatus.Failed;
            else if (started.Contains(s.BatchNumber) && !completed.ContainsKey(s.BatchNumber))
                status = BatchStatus.InProgress;
            else
                status = BatchStatus.Pending;

            return new ScheduledBatchItem
            {
                BatchNumber = s.BatchNumber,
                ScheduledAt = s.ScheduledAt,
                Status = status,
                FundsLoaded = completed.TryGetValue(s.BatchNumber, out var funds) ? funds : null
            };
        }).ToList();

        _scheduledBatches.OnNext(items);
    }

    /// <summary>
    /// Releases all resources used by the orchestrator.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.Debug("CrawlSessionOrchestrator disposing");

        _countdownSubscription?.Dispose();
        _disposables.Dispose();
        _sessionState.Dispose();
        _scheduledBatches.Dispose();
        _countdownTick.Dispose();
        _loadBatchRequested.Dispose();
        _sessionCompleted.Dispose();

        _disposed = true;
    }
}
