using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Service for scheduling fund list sessions with pre-calculated batch timings.
/// </summary>
/// <remarks>
/// <para>
/// This service pre-schedules all batch loads upfront with randomized delays,
/// allowing the ViewModel to query the next scheduled time rather than
/// calculating delays on-the-fly.
/// </para>
/// </remarks>
public interface IFundListSessionScheduler
{
    /// <summary>
    /// Schedules a new fund list session with pre-calculated batch times.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Appends <c>FundListSessionStarted</c> and all <c>FundListBatchScheduled</c> events
    /// to the event store with randomized delays (20-60 seconds between batches).
    /// </para>
    /// </remarks>
    /// <param name="expectedBatchCount">
    /// Number of "next page" clicks to schedule (empirical value, typically 74).
    /// </param>
    /// <returns>The new session's unique correlation ID.</returns>
    FundListSessionId ScheduleSession(int expectedBatchCount);
}
