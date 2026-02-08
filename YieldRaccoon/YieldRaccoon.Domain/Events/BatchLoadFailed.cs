using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when a batch load fails due to an error.
/// </summary>
/// <remarks>
/// <para>
/// This event indicates that the "Visa fler" button click failed or the
/// response could not be processed. This typically triggers <see cref="CrawlSessionFailed"/>.
/// </para>
///
/// <para><strong>Note:</strong></para>
/// <para>
/// "Button not found" when all funds are loaded is NOT a failure -
/// that's a normal completion scenario.
/// </para>
/// </remarks>
[DebuggerDisplay("BatchLoadFailed: Session={SessionId}, Batch={BatchNumber}, Reason={FailureReason} at {OccurredAt}")]
public sealed record BatchLoadFailed : IDomainEvent
{
    /// <summary>
    /// Gets the unique correlation ID for the crawl session.
    /// </summary>
    public required CrawlSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the batch number that failed to load.
    /// </summary>
    public required BatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the reason for the failure.
    /// </summary>
    public required string FailureReason { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="BatchLoadFailed"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The session correlation ID.</param>
    /// <param name="batchNumber">The batch number that failed.</param>
    /// <param name="failureReason">The reason for the failure.</param>
    /// <returns>A new immutable event instance.</returns>
    public static BatchLoadFailed Create(
        CrawlSessionId sessionId,
        BatchNumber batchNumber,
        string failureReason)
    {
        return new BatchLoadFailed
        {
            SessionId = sessionId,
            BatchNumber = batchNumber,
            FailureReason = failureReason,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
