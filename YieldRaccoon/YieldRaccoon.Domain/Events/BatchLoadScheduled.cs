using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when a batch load is scheduled with a delay.
/// </summary>
/// <remarks>
/// <para>
/// This event is created dynamically for each upcoming "Visa fler" click.
/// The delay is randomized between 20-60 seconds to avoid rate limiting.
/// </para>
/// </remarks>
[DebuggerDisplay("BatchLoadScheduled: Session={SessionId}, Batch={BatchNumber}, Delay={DelaySeconds}s at {OccurredAt}")]
public sealed record BatchLoadScheduled : IDomainEvent
{
    /// <summary>
    /// Gets the unique correlation ID for the crawl session.
    /// </summary>
    public required CrawlSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the batch number being scheduled.
    /// </summary>
    public required BatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the delay in seconds before this batch load should start.
    /// </summary>
    public required int DelaySeconds { get; init; }

    /// <summary>
    /// Gets the scheduled start time for this batch.
    /// </summary>
    public DateTimeOffset ScheduledAt => OccurredAt.AddSeconds(DelaySeconds);

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="BatchLoadScheduled"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The session correlation ID.</param>
    /// <param name="batchNumber">The batch number being scheduled.</param>
    /// <param name="delaySeconds">Delay before batch load starts.</param>
    /// <returns>A new immutable event instance.</returns>
    public static BatchLoadScheduled Create(
        CrawlSessionId sessionId,
        BatchNumber batchNumber,
        int delaySeconds)
    {
        return new BatchLoadScheduled
        {
            SessionId = sessionId,
            BatchNumber = batchNumber,
            DelaySeconds = delaySeconds,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
