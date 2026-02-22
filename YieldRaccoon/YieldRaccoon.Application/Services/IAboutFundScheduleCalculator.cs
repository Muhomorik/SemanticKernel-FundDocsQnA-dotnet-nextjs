using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Pure computation service for building and adjusting the pre-calculated
/// session schedule (fund visit timings with randomized delays).
/// </summary>
/// <remarks>
/// All methods are pure computations with no I/O, no async, and no side effects.
/// The randomized delays come from the injected <see cref="IRandomDelayProvider"/>.
/// </remarks>
public interface IAboutFundScheduleCalculator
{
    /// <summary>
    /// Pre-calculates the full session schedule by rolling randomized delays
    /// for each fund's collection steps and inter-page gaps.
    /// </summary>
    /// <param name="funds">Ordered list of funds to visit.</param>
    /// <param name="startTime">Absolute time when the first fund visit should begin.</param>
    /// <param name="getMinimumDelay">
    /// Returns the minimum interaction delay for each step kind.
    /// Typically bound to <c>IAboutFundPageInteractor.GetMinimumDelay</c>.
    /// </param>
    /// <returns>
    /// Ordered list of <see cref="AboutFundCollectionSchedule"/> with pre-calculated
    /// absolute step times and inter-page delays.
    /// </returns>
    List<AboutFundCollectionSchedule> CalculateSessionSchedule(
        IReadOnlyList<AboutFundScheduleItem> funds,
        DateTimeOffset startTime,
        Func<AboutFundCollectionStepKind, TimeSpan> getMinimumDelay);

    /// <summary>
    /// Returns a new schedule list where fund timings from <paramref name="fromOrderBookId"/>
    /// onwards are shifted to chain from <paramref name="baseTime"/>, preserving
    /// original inter-page delays and collection durations. Completed funds are skipped.
    /// </summary>
    /// <param name="fundSchedules">The current schedule list (not mutated).</param>
    /// <param name="fromOrderBookId">The OrderBookId of the first fund to recalculate.</param>
    /// <param name="baseTime">The base time from which to recalculate timings.</param>
    /// <param name="visitStatuses">Per-fund visit statuses; completed funds are skipped.</param>
    /// <returns>
    /// A new list with recalculated timings. Entries before <paramref name="fromOrderBookId"/>
    /// and completed funds are carried over unchanged.
    /// </returns>
    List<AboutFundCollectionSchedule> RecalculateRemainingSchedule(
        IReadOnlyList<AboutFundCollectionSchedule> fundSchedules,
        OrderBookId fromOrderBookId,
        DateTimeOffset baseTime,
        IReadOnlyDictionary<OrderBookId, FundVisitStatus> visitStatuses);
}
