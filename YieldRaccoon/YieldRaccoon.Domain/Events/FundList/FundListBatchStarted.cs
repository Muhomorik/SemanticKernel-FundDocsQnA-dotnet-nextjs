using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.FundList;

/// <summary>
/// Event published when a batch load begins (about to click "Visa fler").
/// </summary>
/// <remarks>
/// <para>
/// This event is raised after the delay timer elapses, just before
/// the JavaScript click on the "Visa fler" button is executed.
/// </para>
/// </remarks>
[DebuggerDisplay("FundListBatchStarted: Session={SessionId}, Batch={BatchNumber} at {OccurredAt}")]
public sealed record FundListBatchStarted : IFundListEvent
{
    /// <summary>
    /// Gets the unique correlation ID for the crawl session.
    /// </summary>
    public required FundListSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the batch number being loaded.
    /// </summary>
    public required FundListBatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="FundListBatchStarted"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The session correlation ID.</param>
    /// <param name="batchNumber">The batch number being loaded.</param>
    /// <returns>A new immutable event instance.</returns>
    public static FundListBatchStarted Create(
        FundListSessionId sessionId,
        FundListBatchNumber batchNumber)
    {
        return new FundListBatchStarted
        {
            SessionId = sessionId,
            BatchNumber = batchNumber,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
