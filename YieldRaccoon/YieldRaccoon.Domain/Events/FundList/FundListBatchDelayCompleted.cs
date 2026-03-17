using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.FundList;

/// <summary>
/// Event published when a delay timer completes before a batch load.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when the Rx.NET timer elapses and the crawler
/// is ready to click the "Visa fler" button.
/// </para>
/// </remarks>
[DebuggerDisplay("FundListBatchDelayCompleted: Session={SessionId}, Batch={BatchNumber} at {OccurredAt}")]
public sealed record FundListBatchDelayCompleted : IFundListEvent
{
    /// <summary>
    /// Gets the unique correlation ID for the crawl session.
    /// </summary>
    public required FundListSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the batch number ready to be loaded.
    /// </summary>
    public required FundListBatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="FundListBatchDelayCompleted"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The session correlation ID.</param>
    /// <param name="batchNumber">The batch number ready to be loaded.</param>
    /// <returns>A new immutable event instance.</returns>
    public static FundListBatchDelayCompleted Create(
        FundListSessionId sessionId,
        FundListBatchNumber batchNumber)
    {
        return new FundListBatchDelayCompleted
        {
            SessionId = sessionId,
            BatchNumber = batchNumber,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
