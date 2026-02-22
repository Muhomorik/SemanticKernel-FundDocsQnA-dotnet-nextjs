using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Pure computation service for building and adjusting session schedules
/// with randomized delays.
/// </summary>
public class AboutFundScheduleCalculator : IAboutFundScheduleCalculator
{
    private readonly ILogger _logger;
    private readonly IRandomDelayProvider _delayProvider;

    public AboutFundScheduleCalculator(ILogger logger, IRandomDelayProvider delayProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

    /// <inheritdoc/>
    public List<AboutFundCollectionSchedule> CalculateSessionSchedule(
        IReadOnlyList<AboutFundScheduleItem> funds,
        DateTimeOffset startTime,
        Func<AboutFundCollectionStepKind, TimeSpan> getMinimumDelay)
    {
        ArgumentNullException.ThrowIfNull(funds);
        ArgumentNullException.ThrowIfNull(getMinimumDelay);

        var currStartTime = startTime;
        var fundSchedules = new List<AboutFundCollectionSchedule>(funds.Count);

        foreach (var fund in funds)
        {
            var steps = new List<AboutFundScheduledStep>();
            var cumulative = TimeSpan.Zero;

            foreach (var kind in AboutFundCollectionStepKinds.All)
            {
                var minDelay = getMinimumDelay(kind);
                cumulative += _delayProvider.NextDelay(minDelay);
                steps.Add(new AboutFundScheduledStep(kind, currStartTime + cumulative));
            }

            // Safety-net timer after last step
            cumulative += _delayProvider.NextDelay();

            var stopTime = currStartTime + cumulative;
            var interPageDelay = _delayProvider.NextDelay();

            fundSchedules.Add(new AboutFundCollectionSchedule
            {
                OrderBookId = fund.OrderBookId,
                StartTime = currStartTime,
                StopTime = stopTime,
                Steps = steps,
                InterPageDelay = interPageDelay
            });

            currStartTime = stopTime + interPageDelay;
        }

        _logger.Info("Pre-calculated session schedule: {0} funds, total duration {1:F0}s",
            fundSchedules.Count,
            fundSchedules.Count > 0
                ? (fundSchedules[^1].StopTime + fundSchedules[^1].InterPageDelay
                    - fundSchedules[0].StartTime).TotalSeconds
                : 0);

        return fundSchedules;
    }

    /// <inheritdoc/>
    public List<AboutFundCollectionSchedule> RecalculateRemainingSchedule(
        IReadOnlyList<AboutFundCollectionSchedule> fundSchedules,
        OrderBookId fromOrderBookId,
        DateTimeOffset baseTime,
        IReadOnlyDictionary<OrderBookId, FundVisitStatus> visitStatuses)
    {
        ArgumentNullException.ThrowIfNull(fundSchedules);
        ArgumentNullException.ThrowIfNull(visitStatuses);

        var fromIndex = FindIndex(fundSchedules, fromOrderBookId);
        if (fromIndex < 0)
            return [.. fundSchedules];

        var result = new List<AboutFundCollectionSchedule>(fundSchedules.Count);
        for (var i = 0; i < fromIndex; i++)
            result.Add(fundSchedules[i]);

        // Use the previous fund's inter-page delay as an initial gap
        var currStart = fromIndex > 0
            ? baseTime + fundSchedules[fromIndex - 1].InterPageDelay
            : baseTime;

        for (var i = fromIndex; i < fundSchedules.Count; i++)
        {
            var entry = fundSchedules[i];

            // Skip completed funds — carry them over unchanged
            if (visitStatuses.TryGetValue(entry.OrderBookId, out var status)
                && status == FundVisitStatus.Completed)
            {
                result.Add(entry);
                continue;
            }

            var duration = entry.TotalDuration;
            var shifted = entry with
            {
                StartTime = currStart,
                StopTime = currStart + duration
            };
            result.Add(shifted);
            currStart = shifted.StopTime + entry.InterPageDelay;
        }

        _logger.Debug("Recalculated schedule from {0}, next start at {1:HH:mm:ss}",
            fromOrderBookId, result[fromIndex].StartTime);

        return result;
    }

    private static int FindIndex(IReadOnlyList<AboutFundCollectionSchedule> schedules, OrderBookId id)
    {
        for (var i = 0; i < schedules.Count; i++)
        {
            if (schedules[i].OrderBookId == id)
                return i;
        }

        return -1;
    }
}
