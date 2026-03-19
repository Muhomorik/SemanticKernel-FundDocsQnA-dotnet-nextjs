using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Pure computation service for building and adjusting the pre-calculated
/// fund list session schedule (batch load timings with randomized delays).
/// </summary>
/// <remarks>
/// All methods are pure computations with no I/O, no async, and no side effects.
/// The randomized delays come from the injected <see cref="IRandomDelayProvider"/>.
/// </remarks>
public interface IFundListScheduleCalculator
{
    /// <summary>
    /// Pre-calculates the full session schedule by rolling randomized delays
    /// for each batch load.
    /// </summary>
    /// <param name="expectedBatchCount">Number of batches to schedule (typically 74).</param>
    /// <param name="startTime">Absolute time when the first batch should fire.</param>
    /// <returns>
    /// Ordered list of <see cref="FundListBatchSchedule"/> with pre-calculated
    /// absolute fire times.
    /// </returns>
    List<FundListBatchSchedule> CalculateSessionSchedule(
        int expectedBatchCount,
        DateTimeOffset startTime);

    /// <summary>
    /// Returns a new schedule list where batch timings from <paramref name="fromBatchNumber"/>
    /// onwards are shifted to chain from <paramref name="baseTime"/>, preserving
    /// original delay durations. Completed batches are skipped.
    /// </summary>
    /// <param name="batchSchedules">The current schedule list (not mutated).</param>
    /// <param name="fromBatchNumber">The first batch number to recalculate.</param>
    /// <param name="baseTime">The base time from which to recalculate timings.</param>
    /// <param name="batchStatuses">Per-batch statuses; completed batches are skipped.</param>
    /// <returns>
    /// A new list with recalculated timings. Entries before <paramref name="fromBatchNumber"/>
    /// and completed batches are carried over unchanged.
    /// </returns>
    List<FundListBatchSchedule> RecalculateRemainingSchedule(
        IReadOnlyList<FundListBatchSchedule> batchSchedules,
        FundListBatchNumber fromBatchNumber,
        DateTimeOffset baseTime,
        IReadOnlyDictionary<FundListBatchNumber, FundListBatchStatus> batchStatuses);
}
