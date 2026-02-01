using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Service for scheduling crawl sessions with pre-calculated batch timings.
/// </summary>
/// <remarks>
/// <para>
/// This service pre-schedules all batch loads upfront with randomized delays,
/// allowing the ViewModel to query the next scheduled time rather than
/// calculating delays on-the-fly.
/// </para>
/// </remarks>
public interface ICrawlSessionScheduler
{
    /// <summary>
    /// Schedules a new crawl session with pre-calculated batch times.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Appends <c>CrawlSessionStarted</c> and all <c>BatchLoadScheduled</c> events
    /// to the event store with randomized delays (20-60 seconds between batches).
    /// </para>
    /// </remarks>
    /// <param name="expectedBatchCount">
    /// Number of "next page" clicks to schedule (empirical value, typically 74).
    /// </param>
    /// <returns>The new session's unique correlation ID.</returns>
    CrawlSessionId ScheduleSession(int expectedBatchCount);
}
