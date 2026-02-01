using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when a delay timer starts before a batch load.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when an Rx.NET timer begins counting down
/// before the next "Visa fler" click. The delay is randomized to avoid rate limiting.
/// </para>
/// </remarks>
[DebuggerDisplay("BatchLoadDelayStarted: Session={SessionId}, Batch={BatchNumber}, Delay={DelaySeconds}s at {OccurredAt}")]
public sealed record BatchLoadDelayStarted : IDomainEvent
{
    /// <summary>
    /// Gets the unique correlation ID for the crawl session.
    /// </summary>
    public required CrawlSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the batch number waiting to be loaded.
    /// </summary>
    public required BatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the delay duration in seconds.
    /// </summary>
    public required int DelaySeconds { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="BatchLoadDelayStarted"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The session correlation ID.</param>
    /// <param name="batchNumber">The batch number waiting to be loaded.</param>
    /// <param name="delaySeconds">The delay duration in seconds.</param>
    /// <returns>A new immutable event instance.</returns>
    public static BatchLoadDelayStarted Create(
        CrawlSessionId sessionId,
        BatchNumber batchNumber,
        int delaySeconds)
    {
        return new BatchLoadDelayStarted
        {
            SessionId = sessionId,
            BatchNumber = batchNumber,
            DelaySeconds = delaySeconds,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
