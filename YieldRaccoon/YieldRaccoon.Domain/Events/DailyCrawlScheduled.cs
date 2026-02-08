using System.Diagnostics;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when the next daily crawl is scheduled approximately 24 hours later.
/// </summary>
/// <remarks>
/// <para>
/// Published after <see cref="CrawlSessionCompleted"/> to schedule the next daily crawl
/// of the entire fund list. The <see cref="ScheduledTime"/> is randomized within a
/// 2-hour evening window (19:00-21:00 UTC) to avoid detection patterns.
/// </para>
///
/// <para><strong>Scheduling strategy:</strong></para>
/// <list type="bullet">
///   <item><description>Randomized time between 19:00-21:00 UTC next day</description></item>
///   <item><description>Spreads crawls across evening window to avoid detection</description></item>
///   <item><description>Uses Rx.NET timer to schedule ~24 hour delay</description></item>
///   <item><description>When timer elapses, <see cref="DailyCrawlReady"/> is published</description></item>
/// </list>
/// </remarks>
[DebuggerDisplay("DailyCrawlScheduled: ScheduledTime={ScheduledTime} at {OccurredAt}")]
public sealed record DailyCrawlScheduled : IDomainEvent
{
    /// <summary>
    /// Gets the scheduled crawl time (randomized between 19:00-21:00 UTC next day).
    /// </summary>
    /// <remarks>
    /// Calculated by the Application layer as next day + random offset between 19:00-21:00 UTC.
    /// </remarks>
    public required DateTimeOffset ScheduledTime { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="DailyCrawlScheduled"/> event with UTC timestamp.
    /// </summary>
    /// <param name="scheduledTime">The scheduled crawl time (19:00-21:00 UTC next day).</param>
    /// <returns>A new immutable event instance.</returns>
    public static DailyCrawlScheduled Create(DateTimeOffset scheduledTime)
    {
        return new DailyCrawlScheduled
        {
            ScheduledTime = scheduledTime,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
