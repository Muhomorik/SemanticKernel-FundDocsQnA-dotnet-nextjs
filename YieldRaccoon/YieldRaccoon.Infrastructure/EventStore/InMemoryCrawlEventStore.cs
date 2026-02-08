using System.Collections.Concurrent;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.EventStore;

/// <summary>
/// In-memory implementation of <see cref="ICrawlEventStore"/> using thread-safe collections.
/// </summary>
/// <remarks>
/// <para>
/// Stores all crawl session events in an append-only list and derives state
/// through LINQ projections. Data is volatile and will be lost when the application restarts.
/// </para>
///
/// <para><strong>Thread safety:</strong></para>
/// <para>
/// Uses locking for append operations and snapshot reads to ensure thread safety.
/// </para>
/// </remarks>
public class InMemoryCrawlEventStore : ICrawlEventStore
{
    private readonly List<IDomainEvent> _events = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Append(IDomainEvent domainEvent)
    {
        lock (_lock)
        {
            _events.Add(domainEvent);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<BatchLoadScheduled> GetPendingBatchLoads(CrawlSessionId sessionId)
    {
        lock (_lock)
        {
            var scheduled = _events.OfType<BatchLoadScheduled>()
                .Where(e => e.SessionId == sessionId)
                .ToList();

            var completedOrFailed = _events.OfType<BatchLoadCompleted>()
                .Where(e => e.SessionId == sessionId)
                .Select(e => e.BatchNumber)
                .Concat(
                    _events.OfType<BatchLoadFailed>()
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
    public BatchLoadScheduled? GetNextScheduledBatch(CrawlSessionId sessionId)
    {
        return GetPendingBatchLoads(sessionId).FirstOrDefault();
    }

    /// <inheritdoc />
    public int GetCompletedBatchCount(CrawlSessionId sessionId)
    {
        lock (_lock)
        {
            return _events.OfType<BatchLoadCompleted>()
                .Count(e => e.SessionId == sessionId);
        }
    }

    /// <inheritdoc />
    public int GetTotalFundsLoaded(CrawlSessionId sessionId)
    {
        lock (_lock)
        {
            return _events.OfType<BatchLoadCompleted>()
                .Where(e => e.SessionId == sessionId)
                .Sum(e => e.FundsInBatch);
        }
    }

    /// <inheritdoc />
    public bool IsSessionActive(CrawlSessionId sessionId)
    {
        lock (_lock)
        {
            var hasStarted = _events.OfType<CrawlSessionStarted>()
                .Any(e => e.SessionId == sessionId);

            if (!hasStarted)
                return false;

            var hasEnded = _events.OfType<CrawlSessionCompleted>()
                .Any(e => e.SessionId == sessionId)
                || _events.OfType<CrawlSessionFailed>()
                    .Any(e => e.SessionId == sessionId)
                || _events.OfType<CrawlSessionCancelled>()
                    .Any(e => e.SessionId == sessionId);

            return !hasEnded;
        }
    }

    /// <inheritdoc />
    public CrawlSessionStarted? GetActiveSession()
    {
        lock (_lock)
        {
            // Get all started sessions in reverse chronological order
            var startedSessions = _events.OfType<CrawlSessionStarted>()
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
    public IReadOnlyList<IDomainEvent> GetSessionEvents(CrawlSessionId sessionId)
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
    public IReadOnlyList<DateTimeOffset> GetBatchLoadTimestamps(CrawlSessionId sessionId)
    {
        lock (_lock)
        {
            return _events.OfType<BatchLoadCompleted>()
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
    private bool IsSessionActiveInternal(CrawlSessionId sessionId)
    {
        var hasEnded = _events.OfType<CrawlSessionCompleted>()
            .Any(e => e.SessionId == sessionId)
            || _events.OfType<CrawlSessionFailed>()
                .Any(e => e.SessionId == sessionId)
            || _events.OfType<CrawlSessionCancelled>()
                .Any(e => e.SessionId == sessionId);

        return !hasEnded;
    }

    /// <summary>
    /// Checks if an event belongs to a specific session.
    /// </summary>
    private static bool IsEventForSession(IDomainEvent domainEvent, CrawlSessionId sessionId)
    {
        return domainEvent switch
        {
            CrawlSessionStarted e => e.SessionId == sessionId,
            CrawlSessionCompleted e => e.SessionId == sessionId,
            CrawlSessionFailed e => e.SessionId == sessionId,
            CrawlSessionCancelled e => e.SessionId == sessionId,
            BatchLoadScheduled e => e.SessionId == sessionId,
            BatchLoadStarted e => e.SessionId == sessionId,
            BatchLoadCompleted e => e.SessionId == sessionId,
            BatchLoadFailed e => e.SessionId == sessionId,
            BatchLoadDelayStarted e => e.SessionId == sessionId,
            BatchLoadDelayCompleted e => e.SessionId == sessionId,
            _ => false
        };
    }
}
