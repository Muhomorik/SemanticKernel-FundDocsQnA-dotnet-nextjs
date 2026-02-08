using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when a batch of funds is successfully loaded.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when the WebView2 interceptor captures a successful
/// response containing fund data after a "Visa fler" button click.
/// </para>
///
/// <para><strong>Note:</strong></para>
/// <para>
/// <see cref="SessionId"/> is nullable to support manual clicks outside of a crawl session.
/// When null, the event is used for database updates only (no session tracking).
/// </para>
/// </remarks>
[DebuggerDisplay("BatchLoadCompleted: Session={SessionId}, Batch={BatchNumber}, Funds={FundsInBatch}, Total={TotalFundsLoaded} at {OccurredAt}")]
public sealed record BatchLoadCompleted : IDomainEvent
{
    /// <summary>
    /// Gets the unique correlation ID for the crawl session, or null for manual clicks.
    /// </summary>
    public CrawlSessionId? SessionId { get; init; }

    /// <summary>
    /// Gets the batch number that was loaded.
    /// </summary>
    public required BatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the number of funds loaded in this batch.
    /// </summary>
    public required int FundsInBatch { get; init; }

    /// <summary>
    /// Gets the total number of funds loaded so far (including this batch).
    /// </summary>
    public required int TotalFundsLoaded { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="BatchLoadCompleted"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The session correlation ID, or null for manual clicks.</param>
    /// <param name="batchNumber">The batch number that was loaded.</param>
    /// <param name="fundsInBatch">Number of funds loaded in this batch.</param>
    /// <param name="totalFundsLoaded">Total funds loaded so far.</param>
    /// <returns>A new immutable event instance.</returns>
    public static BatchLoadCompleted Create(
        CrawlSessionId? sessionId,
        BatchNumber batchNumber,
        int fundsInBatch,
        int totalFundsLoaded)
    {
        return new BatchLoadCompleted
        {
            SessionId = sessionId,
            BatchNumber = batchNumber,
            FundsInBatch = fundsInBatch,
            TotalFundsLoaded = totalFundsLoaded,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
