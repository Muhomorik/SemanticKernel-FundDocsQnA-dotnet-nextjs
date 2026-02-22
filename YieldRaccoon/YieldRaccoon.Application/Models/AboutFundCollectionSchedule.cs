using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Pre-calculated schedule for a single fund page visit, including interaction step timings
/// and the inter-page delay before the next fund.
/// </summary>
/// <remarks>
/// Built by the orchestrator in <c>StartSessionAsync</c> which owns all scheduling policy:
/// it queries <c>IAboutFundPageInteractor.GetMinimumDelay</c> for each step kind,
/// rolls randomized delays via <c>IRandomDelayProvider</c>, and produces the full
/// session timeline upfront. The collector receives this as input to
/// <c>BeginCollection</c> and schedules timers at the prescribed absolute times.
/// </remarks>
public record AboutFundCollectionSchedule
{
    /// <summary>
    /// Identity of the fund to visit.
    /// </summary>
    public required OrderBookId OrderBookId { get; init; }

    /// <summary>
    /// When navigation to this fund's detail page fires.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// <see cref="StartTime"/> + total step durations (including safety-net timer).
    /// When the collection is expected to finish.
    /// </summary>
    public required DateTimeOffset StopTime { get; init; }

    /// <summary>
    /// Pre-calculated interaction steps with absolute fire times.
    /// </summary>
    public required IReadOnlyList<AboutFundScheduledStep> Steps { get; init; }

    /// <summary>
    /// Random delay after this fund's collection completes, before the next fund's navigation.
    /// </summary>
    public required TimeSpan InterPageDelay { get; init; }

    /// <summary>
    /// Total collection duration (from <see cref="StartTime"/> to <see cref="StopTime"/>).
    /// </summary>
    public TimeSpan TotalDuration => StopTime - StartTime;
}
