using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when a crawl session fails due to an error.
/// </summary>
/// <remarks>
/// <para>
/// This event indicates that the session could not complete due to a technical error
/// (e.g., "Visa fler" button click failed, timeout, network error).
/// </para>
///
/// <para><strong>Note:</strong></para>
/// <para>
/// "Button not found" when all funds are already loaded is NOT a failure -
/// that triggers <see cref="CrawlSessionCompleted"/> instead.
/// </para>
/// </remarks>
[DebuggerDisplay("CrawlSessionFailed: Session={SessionId}, Reason={FailureReason}, LastBatch={LastCompletedBatch} at {OccurredAt}")]
public sealed record CrawlSessionFailed : IDomainEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this crawl session.
    /// </summary>
    public required CrawlSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the reason for the failure.
    /// </summary>
    public required string FailureReason { get; init; }

    /// <summary>
    /// Gets the last successfully completed batch before the failure occurred.
    /// </summary>
    /// <remarks>
    /// Null if failure occurred before any batch was completed.
    /// </remarks>
    public BatchNumber? LastCompletedBatch { get; init; }

    /// <summary>
    /// Gets the timestamp when the session started.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the failure occurred.
    /// </summary>
    public required DateTimeOffset FailedAt { get; init; }

    /// <summary>
    /// Gets the duration from session start until failure.
    /// </summary>
    public TimeSpan Duration => FailedAt - StartedAt;

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="CrawlSessionFailed"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The unique session correlation ID.</param>
    /// <param name="failureReason">The reason for the failure.</param>
    /// <param name="startedAt">Timestamp when session started.</param>
    /// <param name="lastCompletedBatch">Last successful batch, or null if none.</param>
    /// <returns>A new immutable event instance.</returns>
    public static CrawlSessionFailed Create(
        CrawlSessionId sessionId,
        string failureReason,
        DateTimeOffset startedAt,
        BatchNumber? lastCompletedBatch = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new CrawlSessionFailed
        {
            SessionId = sessionId,
            FailureReason = failureReason,
            LastCompletedBatch = lastCompletedBatch,
            StartedAt = startedAt,
            FailedAt = now,
            OccurredAt = now
        };
    }
}
