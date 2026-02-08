using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when a crawl session completes successfully after loading all funds.
/// </summary>
/// <remarks>
/// <para>
/// This event indicates that all funds were loaded from the paginated list
/// (the "Visa fler" button is no longer available).
/// </para>
///
/// <para><strong>Post-actions (Application Layer):</strong></para>
/// <list type="bullet">
///   <item><description>Publish <see cref="DailyCrawlScheduled"/> to schedule next crawl ~24 hours later (19:00-21:00 window).</description></item>
///   <item><description>Persist fund data to database.</description></item>
/// </list>
/// </remarks>
[DebuggerDisplay("CrawlSessionCompleted: Session={SessionId}, Funds={TotalFundsLoaded}, Batches={TotalBatchesLoaded}, Duration={Duration} at {OccurredAt}")]
public sealed record CrawlSessionCompleted : IDomainEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this crawl session.
    /// </summary>
    public required CrawlSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the total number of funds loaded during the session.
    /// </summary>
    public required int TotalFundsLoaded { get; init; }

    /// <summary>
    /// Gets the total number of batches (Visa fler clicks) executed.
    /// </summary>
    public required int TotalBatchesLoaded { get; init; }

    /// <summary>
    /// Gets the timestamp when the session started (from CrawlSessionStarted.OccurredAt).
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the session completed.
    /// </summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Gets the computed total duration from StartedAt to CompletedAt.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Gets the timestamps of each "Visa fler" click, ordered chronologically.
    /// </summary>
    /// <remarks>
    /// Count equals TotalBatchesLoaded. Each timestamp marks when a batch load was initiated.
    /// </remarks>
    public required IReadOnlyList<DateTimeOffset> BatchLoadTimestamps { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="CrawlSessionCompleted"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The unique session correlation ID.</param>
    /// <param name="totalFundsLoaded">Total funds loaded during the session.</param>
    /// <param name="totalBatchesLoaded">Total batches (Visa fler clicks) executed.</param>
    /// <param name="startedAt">Timestamp when session started.</param>
    /// <param name="batchLoadTimestamps">Timestamps of each batch load.</param>
    /// <returns>A new immutable event instance.</returns>
    public static CrawlSessionCompleted Create(
        CrawlSessionId sessionId,
        int totalFundsLoaded,
        int totalBatchesLoaded,
        DateTimeOffset startedAt,
        IReadOnlyList<DateTimeOffset> batchLoadTimestamps)
    {
        var now = DateTimeOffset.UtcNow;
        return new CrawlSessionCompleted
        {
            SessionId = sessionId,
            TotalFundsLoaded = totalFundsLoaded,
            TotalBatchesLoaded = totalBatchesLoaded,
            StartedAt = startedAt,
            CompletedAt = now,
            BatchLoadTimestamps = batchLoadTimestamps,
            OccurredAt = now
        };
    }
}
