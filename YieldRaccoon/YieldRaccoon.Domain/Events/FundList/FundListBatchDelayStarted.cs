using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.FundList;

/// <summary>
/// Event published when a delay timer starts before a batch load.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when an Rx.NET timer begins counting down
/// before the next "Visa fler" click. The delay is randomized to avoid rate limiting.
/// </para>
/// </remarks>
[DebuggerDisplay("FundListBatchDelayStarted: Session={SessionId}, Batch={BatchNumber}, Delay={DelaySeconds}s at {OccurredAt}")]
public sealed record FundListBatchDelayStarted : IFundListEvent
{
    /// <summary>
    /// Gets the unique correlation ID for the crawl session.
    /// </summary>
    public required FundListSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the batch number waiting to be loaded.
    /// </summary>
    public required FundListBatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the delay duration in seconds.
    /// </summary>
    public required int DelaySeconds { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="FundListBatchDelayStarted"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The session correlation ID.</param>
    /// <param name="batchNumber">The batch number waiting to be loaded.</param>
    /// <param name="delaySeconds">The delay duration in seconds.</param>
    /// <returns>A new immutable event instance.</returns>
    public static FundListBatchDelayStarted Create(
        FundListSessionId sessionId,
        FundListBatchNumber batchNumber,
        int delaySeconds)
    {
        return new FundListBatchDelayStarted
        {
            SessionId = sessionId,
            BatchNumber = batchNumber,
            DelaySeconds = delaySeconds,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
