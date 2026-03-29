using System.Collections.Concurrent;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events.FundList;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.EventStore;

/// <summary>
/// In-memory implementation of <see cref="IFundListEventStore"/> using thread-safe collections.
/// </summary>
/// <remarks>
/// <para>
/// Stores all fund list session events in an append-only list and derives state
/// through LINQ projections. Data is volatile and will be lost when the application restarts.
/// </para>
///
/// <para><strong>Thread safety:</strong></para>
/// <para>
/// Uses locking for append operations and snapshot reads to ensure thread safety.
/// </para>
/// </remarks>
public class InMemoryFundListEventStore : IFundListEventStore
{
    private readonly List<IFundListEvent> _events = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Append(IFundListEvent domainEvent)
    {
        lock (_lock)
        {
            _events.Add(domainEvent);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<FundListBatchScheduled> GetPendingBatchLoads(FundListSessionId sessionId)
    {
        lock (_lock)
        {
            var scheduled = _events.OfType<FundListBatchScheduled>()
                .Where(e => e.SessionId == sessionId)
                .ToList();

            var completedOrFailed = _events.OfType<FundListBatchCompleted>()
                .Where(e => e.SessionId == sessionId)
                .Select(e => e.BatchNumber)
                .Concat(
                    _events.OfType<FundListBatchFailed>()
                        .Where(e => e.SessionId == sessionId)
                        .Select(e => e.BatchNumber))
                .ToHashSet();

            return scheduled
                .Where(s => !completedOrFailed.Contains(s.BatchNumber))
                .OrderBy(s => s.ScheduledAt)
                .ToList();
        }
    }

    /// <inheritdoc />
    public FundListBatchScheduled? GetNextScheduledBatch(FundListSessionId sessionId)
    {
        return GetPendingBatchLoads(sessionId).FirstOrDefault();
    }

    /// <inheritdoc />
    public int GetCompletedBatchCount(FundListSessionId sessionId)
    {
        lock (_lock)
        {
            return _events.OfType<FundListBatchCompleted>()
                .Count(e => e.SessionId == sessionId);
        }
    }

    /// <inheritdoc />
    public int GetTotalFundsLoaded(FundListSessionId sessionId)
    {
        lock (_lock)
        {
            return _events.OfType<FundListBatchCompleted>()
                .Where(e => e.SessionId == sessionId)
                .Sum(e => e.FundsInBatch);
        }
    }

    /// <inheritdoc />
    public bool IsSessionActive(FundListSessionId sessionId)
    {
        lock (_lock)
        {
            var hasStarted = _events.OfType<FundListSessionStarted>()
                .Any(e => e.SessionId == sessionId);

            if (!hasStarted)
                return false;

            var hasEnded = _events.OfType<FundListSessionCompleted>()
                .Any(e => e.SessionId == sessionId)
                || _events.OfType<FundListSessionFailed>()
                    .Any(e => e.SessionId == sessionId)
                || _events.OfType<FundListSessionCancelled>()
                    .Any(e => e.SessionId == sessionId);

            return !hasEnded;
        }
    }

    /// <inheritdoc />
    public FundListSessionStarted? GetActiveSession()
    {
        lock (_lock)
        {
            // Get all started sessions in reverse chronological order
            var startedSessions = _events.OfType<FundListSessionStarted>()
                .OrderByDescending(e => e.OccurredAt)
                .ToList();

            // Find the first session that hasn't ended
            foreach (var session in startedSessions)
            {
                if (IsSessionActiveInternal(session.SessionId))
                {
                    return session;
                }
            }

            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IFundListEvent> GetSessionEvents(FundListSessionId sessionId)
    {
        lock (_lock)
        {
            return _events
                .Where(e => IsEventForSession(e, sessionId))
                .OrderBy(e => e.OccurredAt)
                .ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<DateTimeOffset> GetBatchLoadTimestamps(FundListSessionId sessionId)
    {
        lock (_lock)
        {
            return _events.OfType<FundListBatchCompleted>()
                .Where(e => e.SessionId == sessionId)
                .OrderBy(e => e.OccurredAt)
                .Select(e => e.OccurredAt)
                .ToList();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }

    /// <summary>
    /// Internal helper to check session active status without re-acquiring lock.
    /// </summary>
    private bool IsSessionActiveInternal(FundListSessionId sessionId)
    {
        var hasEnded = _events.OfType<FundListSessionCompleted>()
            .Any(e => e.SessionId == sessionId)
            || _events.OfType<FundListSessionFailed>()
                .Any(e => e.SessionId == sessionId)
            || _events.OfType<FundListSessionCancelled>()
                .Any(e => e.SessionId == sessionId);

        return !hasEnded;
    }

    /// <summary>
    /// Checks if an event belongs to a specific session.
    /// </summary>
    private static bool IsEventForSession(IFundListEvent domainEvent, FundListSessionId sessionId)
    {
        return domainEvent switch
        {
            FundListSessionStarted e => e.SessionId == sessionId,
            FundListSessionCompleted e => e.SessionId == sessionId,
            FundListSessionFailed e => e.SessionId == sessionId,
            FundListSessionCancelled e => e.SessionId == sessionId,
            FundListBatchScheduled e => e.SessionId == sessionId,
            FundListBatchStarted e => e.SessionId == sessionId,
            FundListBatchCompleted e => e.SessionId == sessionId,
            FundListBatchFailed e => e.SessionId == sessionId,
            FundListBatchDelayStarted e => e.SessionId == sessionId,
            FundListBatchDelayCompleted e => e.SessionId == sessionId,
            _ => false
        };
    }
}
