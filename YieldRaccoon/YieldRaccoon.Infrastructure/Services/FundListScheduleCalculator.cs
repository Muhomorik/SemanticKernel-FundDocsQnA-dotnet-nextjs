using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Pure computation service for building and adjusting fund list session schedules
/// with randomized delays.
/// </summary>
public class FundListScheduleCalculator : IFundListScheduleCalculator
{
    private readonly ILogger _logger;
    private readonly IRandomDelayProvider _delayProvider;

    /// <summary>
    /// Default expected number of batches (empirical value based on typical fund list size).
    /// </summary>
    public const int DefaultExpectedBatchCount = 74;

    public FundListScheduleCalculator(ILogger logger, IRandomDelayProvider delayProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

    /// <inheritdoc/>
    public List<FundListBatchSchedule> CalculateSessionSchedule(
        int expectedBatchCount,
        DateTimeOffset startTime)
    {
        if (expectedBatchCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(expectedBatchCount),
                expectedBatchCount,
                "Expected batch count must be positive.");

        var schedules = new List<FundListBatchSchedule>(expectedBatchCount);
        var currentTime = startTime;

        for (var i = 1; i <= expectedBatchCount; i++)
        {
            var delay = _delayProvider.NextDelay();

            schedules.Add(new FundListBatchSchedule
            {
                BatchNumber = new FundListBatchNumber(i),
                ScheduledAt = currentTime + delay,
                DelayBeforeBatch = delay
            });

            currentTime += delay;
        }

        _logger.Info("Pre-calculated session schedule: {0} batches, total duration {1:F0}s",
            schedules.Count,
            schedules.Count > 0
                ? (schedules[^1].ScheduledAt - schedules[0].ScheduledAt + schedules[0].DelayBeforeBatch).TotalSeconds
                : 0);

        return schedules;
    }

    /// <inheritdoc/>
    public List<FundListBatchSchedule> RecalculateRemainingSchedule(
        IReadOnlyList<FundListBatchSchedule> batchSchedules,
        FundListBatchNumber fromBatchNumber,
        DateTimeOffset baseTime,
        IReadOnlyDictionary<FundListBatchNumber, FundListBatchStatus> batchStatuses)
    {
        ArgumentNullException.ThrowIfNull(batchSchedules);
        ArgumentNullException.ThrowIfNull(batchStatuses);

        var fromIndex = FindIndex(batchSchedules, fromBatchNumber);
        if (fromIndex < 0)
            return [.. batchSchedules];

        var result = new List<FundListBatchSchedule>(batchSchedules.Count);
        for (var i = 0; i < fromIndex; i++)
            result.Add(batchSchedules[i]);

        var currentTime = baseTime;

        for (var i = fromIndex; i < batchSchedules.Count; i++)
        {
            var entry = batchSchedules[i];

            // Skip completed batches — carry them over unchanged
            if (batchStatuses.TryGetValue(entry.BatchNumber, out var status)
                && status == FundListBatchStatus.Completed)
            {
                result.Add(entry);
                continue;
            }

            var shifted = entry with
            {
                ScheduledAt = currentTime + entry.DelayBeforeBatch
            };
            result.Add(shifted);
            currentTime = shifted.ScheduledAt;
        }

        _logger.Debug("Recalculated schedule from batch {0}, next fire at {1:HH:mm:ss}",
            fromBatchNumber.Value, result[fromIndex].ScheduledAt);

        return result;
    }

    private static int FindIndex(IReadOnlyList<FundListBatchSchedule> schedules, FundListBatchNumber batchNumber)
    {
        for (var i = 0; i < schedules.Count; i++)
        {
            if (schedules[i].BatchNumber == batchNumber)
                return i;
        }

        return -1;
    }
}
