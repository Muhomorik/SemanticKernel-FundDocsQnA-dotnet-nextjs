using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events.AboutFund;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.EventStore;

/// <summary>
/// In-memory implementation of <see cref="IAboutFundEventStore"/> using thread-safe collections.
/// </summary>
/// <remarks>
/// <para>
/// Stores all about-fund browsing session events in an append-only list and derives state
/// through LINQ projections. Data is volatile and will be lost when the application restarts.
/// </para>
/// <para><strong>Thread safety:</strong></para>
/// <para>
/// Uses locking for append operations and snapshot reads to ensure thread safety.
/// </para>
/// </remarks>
public class InMemoryAboutFundEventStore : IAboutFundEventStore
{
    private readonly List<IAboutFundEvent> _events = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Append(IAboutFundEvent aboutFundEvent)
    {
        lock (_lock)
        {
            _events.Add(aboutFundEvent);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IAboutFundEvent> GetSessionEvents(AboutFundSessionId sessionId)
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
    public IReadOnlyList<IAboutFundEvent> GetAllEvents()
    {
        lock (_lock)
        {
            return _events.OrderBy(e => e.OccurredAt).ToList();
        }
    }

    /// <inheritdoc />
    public int GetCompletedNavigationCount(AboutFundSessionId sessionId)
    {
        lock (_lock)
        {
            return _events.OfType<AboutFundNavigationCompleted>()
                .Count(e => e.SessionId == sessionId);
        }
    }

    /// <inheritdoc />
    public bool IsSessionActive(AboutFundSessionId sessionId)
    {
        lock (_lock)
        {
            var hasStarted = _events.OfType<AboutFundSessionStarted>()
                .Any(e => e.SessionId == sessionId);

            if (!hasStarted)
                return false;

            return !HasSessionEnded(sessionId);
        }
    }

    /// <inheritdoc />
    public AboutFundSessionStarted? GetActiveSession()
    {
        lock (_lock)
        {
            var startedSessions = _events.OfType<AboutFundSessionStarted>()
                .OrderByDescending(e => e.OccurredAt)
                .ToList();

            foreach (var session in startedSessions)
            {
                if (!HasSessionEnded(session.SessionId))
                {
                    return session;
                }
            }

            return null;
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
    /// Internal helper to check if session has ended without re-acquiring lock.
    /// </summary>
    private bool HasSessionEnded(AboutFundSessionId sessionId)
    {
        return _events.OfType<AboutFundSessionCompleted>()
                .Any(e => e.SessionId == sessionId)
            || _events.OfType<AboutFundSessionCancelled>()
                .Any(e => e.SessionId == sessionId);
    }

    /// <summary>
    /// Checks if an event belongs to a specific session.
    /// </summary>
    private static bool IsEventForSession(IAboutFundEvent aboutFundEvent, AboutFundSessionId sessionId)
    {
        return aboutFundEvent switch
        {
            AboutFundSessionStarted e => e.SessionId == sessionId,
            AboutFundNavigationStarted e => e.SessionId == sessionId,
            AboutFundNavigationCompleted e => e.SessionId == sessionId,
            AboutFundNavigationFailed e => e.SessionId == sessionId,
            AboutFundSessionCompleted e => e.SessionId == sessionId,
            AboutFundSessionCancelled e => e.SessionId == sessionId,
            _ => false
        };
    }
}
