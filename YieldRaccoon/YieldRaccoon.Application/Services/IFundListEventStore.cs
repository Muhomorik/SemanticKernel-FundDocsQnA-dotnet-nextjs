using YieldRaccoon.Domain.Events.FundList;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Event store interface for append-only fund list session events with query projections.
/// </summary>
/// <remarks>
/// <para>
/// This store serves two purposes:
/// <list type="number">
///   <item><description>Append-only event log for all fund list session events</description></item>
///   <item><description>Query projections to derive state from event history</description></item>
/// </list>
/// </para>
///
/// <para><strong>Pending batch calculation:</strong></para>
/// <para>
/// Pending batches = Scheduled batches - (Completed + Failed batches)
/// </para>
/// </remarks>
public interface IFundListEventStore
{
    /// <summary>
    /// Appends a fund list event to the event store.
    /// </summary>
    /// <param name="domainEvent">The event to append.</param>
    void Append(IFundListEvent domainEvent);

    /// <summary>
    /// Gets all pending (not yet completed or failed) batch loads for a session.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <returns>Pending batch loads ordered by scheduled time.</returns>
    IReadOnlyList<FundListBatchScheduled> GetPendingBatchLoads(FundListSessionId sessionId);

    /// <summary>
    /// Gets the next scheduled batch load for a session.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <returns>The next scheduled batch, or null if none pending.</returns>
    FundListBatchScheduled? GetNextScheduledBatch(FundListSessionId sessionId);

    /// <summary>
    /// Gets the count of completed batches for a session.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <returns>The number of completed batches.</returns>
    int GetCompletedBatchCount(FundListSessionId sessionId);

    /// <summary>
    /// Gets the total number of funds loaded so far in a session.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <returns>Total funds loaded across all completed batches.</returns>
    int GetTotalFundsLoaded(FundListSessionId sessionId);

    /// <summary>
    /// Checks if a session is currently active (started but not completed/failed/cancelled).
    /// </summary>
    /// <param name="sessionId">The session to check.</param>
    /// <returns>True if the session is active, false otherwise.</returns>
    bool IsSessionActive(FundListSessionId sessionId);

    /// <summary>
    /// Gets the currently active fund list session, if any.
    /// </summary>
    /// <returns>The active session's start event, or null if no session is active.</returns>
    FundListSessionStarted? GetActiveSession();

    /// <summary>
    /// Gets all events for a specific session.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <returns>All events for the session in chronological order.</returns>
    IReadOnlyList<IFundListEvent> GetSessionEvents(FundListSessionId sessionId);

    /// <summary>
    /// Gets all batch load completion timestamps for a session.
    /// </summary>
    /// <param name="sessionId">The session to query.</param>
    /// <returns>Timestamps of each completed batch load, ordered chronologically.</returns>
    IReadOnlyList<DateTimeOffset> GetBatchLoadTimestamps(FundListSessionId sessionId);

    /// <summary>
    /// Clears all events from the store.
    /// </summary>
    void Clear();
}
