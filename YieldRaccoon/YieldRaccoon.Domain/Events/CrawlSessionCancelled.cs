using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when a crawl session is cancelled by the user.
/// </summary>
/// <remarks>
/// <para>
/// This event indicates that the user intentionally stopped the crawl session
/// (e.g., clicked "Stop Crawling" button or closed the application).
/// </para>
///
/// <para><strong>Post-actions (Application Layer):</strong></para>
/// <list type="bullet">
///   <item><description>Persist funds loaded so far to database.</description></item>
///   <item><description>Stop any pending timers.</description></item>
///   <item><description>Update UI to show "Start Crawling" button again.</description></item>
/// </list>
/// </remarks>
[DebuggerDisplay("CrawlSessionCancelled: Session={SessionId}, FundsLoaded={FundsLoadedSoFar}, Reason={Reason} at {OccurredAt}")]
public sealed record CrawlSessionCancelled : IDomainEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this crawl session.
    /// </summary>
    public required CrawlSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the number of funds loaded before cancellation.
    /// </summary>
    public required int FundsLoadedSoFar { get; init; }

    /// <summary>
    /// Gets the reason for cancellation.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the timestamp when the session started.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the session was cancelled.
    /// </summary>
    public required DateTimeOffset CancelledAt { get; init; }

    /// <summary>
    /// Gets the duration from session start until cancellation.
    /// </summary>
    public TimeSpan Duration => CancelledAt - StartedAt;

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="CrawlSessionCancelled"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The unique session correlation ID.</param>
    /// <param name="fundsLoadedSoFar">Number of funds loaded before cancellation.</param>
    /// <param name="reason">The reason for cancellation.</param>
    /// <param name="startedAt">Timestamp when session started.</param>
    /// <returns>A new immutable event instance.</returns>
    public static CrawlSessionCancelled Create(
        CrawlSessionId sessionId,
        int fundsLoadedSoFar,
        string reason,
        DateTimeOffset startedAt)
    {
        var now = DateTimeOffset.UtcNow;
        return new CrawlSessionCancelled
        {
            SessionId = sessionId,
            FundsLoadedSoFar = fundsLoadedSoFar,
            Reason = reason,
            StartedAt = startedAt,
            CancelledAt = now,
            OccurredAt = now
        };
    }
}
