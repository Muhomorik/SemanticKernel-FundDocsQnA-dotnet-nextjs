using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when a new crawl session begins to load all funds from the list page.
/// </summary>
/// <remarks>
/// <para>
/// A crawl session represents the complete workflow of clicking "Visa fler" repeatedly
/// to load all funds from a fund provider's paginated fund list page.
/// </para>
///
/// <para><strong>Dynamic batch count:</strong></para>
/// <para>
/// <see cref="EstimatedBatchCount"/> is calculated from current data but the actual
/// number of batches may differ if funds are added/removed during the session.
/// The crawler continues until "Visa fler" button is no longer available.
/// </para>
/// </remarks>
[DebuggerDisplay("CrawlSessionStarted: Session={SessionId}, Funds={TotalFundCount}, Batches≈{EstimatedBatchCount}, Duration≈{EstimatedDurationSeconds}s at {OccurredAt}")]
public sealed record CrawlSessionStarted : IDomainEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this crawl session.
    /// </summary>
    public required CrawlSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the total number of funds available (from "Visar X av Y" metadata).
    /// </summary>
    /// <remarks>
    /// This is a dynamic value captured when the session starts.
    /// The actual total may change during crawling if funds are added/removed.
    /// </remarks>
    public required int TotalFundCount { get; init; }

    /// <summary>
    /// Gets the number of funds already loaded before the session started.
    /// </summary>
    /// <remarks>
    /// Typically 20 (the first batch that auto-loads when the page opens).
    /// </remarks>
    public required int CurrentlyLoaded { get; init; }

    /// <summary>
    /// Gets the estimated number of "Visa fler" clicks needed to load all remaining funds.
    /// </summary>
    /// <remarks>
    /// Calculated as: Ceiling((TotalFundCount - CurrentlyLoaded) / 20.0)
    /// This is an estimate; the actual number may differ.
    /// </remarks>
    public required int EstimatedBatchCount { get; init; }

    /// <summary>
    /// Gets the estimated total duration in seconds (for UI progress display).
    /// </summary>
    /// <remarks>
    /// Calculated as: EstimatedBatchCount × AverageDelaySeconds (typically 40s average).
    /// </remarks>
    public required int EstimatedDurationSeconds { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="CrawlSessionStarted"/> event with UTC timestamp.
    /// </summary>
    /// <param name="sessionId">The unique session correlation ID.</param>
    /// <param name="totalFundCount">Total funds available from pagination metadata.</param>
    /// <param name="currentlyLoaded">Funds already loaded before session started.</param>
    /// <param name="estimatedBatchCount">Estimated number of "Visa fler" clicks needed.</param>
    /// <param name="estimatedDurationSeconds">Estimated total duration in seconds.</param>
    /// <returns>A new immutable event instance.</returns>
    public static CrawlSessionStarted Create(
        CrawlSessionId sessionId,
        int totalFundCount,
        int currentlyLoaded,
        int estimatedBatchCount,
        int estimatedDurationSeconds)
    {
        return new CrawlSessionStarted
        {
            SessionId = sessionId,
            TotalFundCount = totalFundCount,
            CurrentlyLoaded = currentlyLoaded,
            EstimatedBatchCount = estimatedBatchCount,
            EstimatedDurationSeconds = estimatedDurationSeconds,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
