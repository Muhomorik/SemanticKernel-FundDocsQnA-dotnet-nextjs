using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Events;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Schedules crawl sessions with pre-calculated batch timings using randomized delays.
/// </summary>
/// <remarks>
/// <para>
/// All batch loads are scheduled upfront when the session starts, with cumulative
/// delays calculated from the session start time. Each batch has a random delay
/// of 20-60 seconds added to the previous batch's scheduled time.
/// </para>
/// </remarks>
public class CrawlSessionScheduler : ICrawlSessionScheduler
{
    private readonly ICrawlEventStore _eventStore;
    private readonly Random _random = new();

    /// <summary>
    /// Minimum delay in seconds between batch loads.
    /// </summary>
    public const int MinDelaySeconds = 10;

    /// <summary>
    /// Maximum delay in seconds between batch loads.
    /// </summary>
    public const int MaxDelaySeconds = 20;

    /// <summary>
    /// Default expected number of batches (empirical value based on typical fund list size).
    /// </summary>
    public const int DefaultExpectedBatchCount = 74;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrawlSessionScheduler"/> class.
    /// </summary>
    /// <param name="eventStore">The event store for appending scheduled events.</param>
    public CrawlSessionScheduler(ICrawlEventStore eventStore)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    /// <inheritdoc/>
    public CrawlSessionId ScheduleSession(int expectedBatchCount)
    {
        if (expectedBatchCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(expectedBatchCount),
                expectedBatchCount,
                "Expected batch count must be positive.");

        var sessionId = CrawlSessionId.NewId();
        var totalSeconds = 0;
        var cumulativeDelay = 0;

        // Pre-schedule all batches with randomized delays
        // Each batch's ScheduledAt is relative to session start (cumulative delay)
        for (var i = 1; i <= expectedBatchCount; i++)
        {
            var delaySeconds = _random.Next(MinDelaySeconds, MaxDelaySeconds + 1);
            cumulativeDelay += delaySeconds;
            totalSeconds += delaySeconds;

            _eventStore.Append(BatchLoadScheduled.Create(
                sessionId,
                new BatchNumber(i),
                cumulativeDelay));
        }

        // Append session started event
        _eventStore.Append(CrawlSessionStarted.Create(
            sessionId,
            0, // Unknown upfront
            0, // Unknown upfront
            expectedBatchCount,
            totalSeconds));

        return sessionId;
    }
}