using YieldRaccoon.Domain.Events.AboutFund;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Event store interface for append-only about-fund browsing session events with query projections.
/// </summary>
/// <remarks>
/// <para>
/// Independent from <see cref="ICrawlEventStore"/> to maintain bounded context separation.
/// </para>
/// </remarks>
public interface IAboutFundEventStore
{
    /// <summary>
    /// Appends an about-fund event to the event store.
    /// </summary>
    /// <param name="aboutFundEvent">The event to append.</param>
    void Append(IAboutFundEvent aboutFundEvent);

    /// <summary>
    /// Gets all events for a specific session in chronological order.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <returns>All events for the session ordered by OccurredAt.</returns>
    IReadOnlyList<IAboutFundEvent> GetSessionEvents(AboutFundSessionId sessionId);

    /// <summary>
    /// Gets all events from all sessions in chronological order.
    /// </summary>
    /// <returns>All events ordered by OccurredAt.</returns>
    IReadOnlyList<IAboutFundEvent> GetAllEvents();

    /// <summary>
    /// Gets the count of completed navigations for a session.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <returns>The number of completed fund page navigations.</returns>
    int GetCompletedNavigationCount(AboutFundSessionId sessionId);

    /// <summary>
    /// Checks if a session is currently active (started but not completed/cancelled).
    /// </summary>
    /// <param name="sessionId">The session to check.</param>
    /// <returns>True if the session is active, false otherwise.</returns>
    bool IsSessionActive(AboutFundSessionId sessionId);

    /// <summary>
    /// Gets the currently active browsing session, if any.
    /// </summary>
    /// <returns>The active session's start event, or null if no session is active.</returns>
    AboutFundSessionStarted? GetActiveSession();

    /// <summary>
    /// Clears all events from the store.
    /// </summary>
    void Clear();
}
