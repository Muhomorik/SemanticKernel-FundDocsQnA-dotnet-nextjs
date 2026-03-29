using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Pre-calculated timing for a single batch load within a fund list session.
/// </summary>
/// <remarks>
/// Analogous to <c>AboutFundCollectionSchedule</c> but simpler — each batch
/// is a single "click Show more" action with no sub-steps.
/// All timings are absolute and computed upfront by <c>IFundListScheduleCalculator</c>.
/// </remarks>
public sealed record FundListBatchSchedule
{
    /// <summary>
    /// Gets the 1-based batch number.
    /// </summary>
    public required FundListBatchNumber BatchNumber { get; init; }

    /// <summary>
    /// Gets the absolute time when this batch load should fire.
    /// </summary>
    public required DateTimeOffset ScheduledAt { get; init; }

    /// <summary>
    /// Gets the randomized delay before this batch (relative to the previous batch's completion).
    /// </summary>
    public required TimeSpan DelayBeforeBatch { get; init; }
}
