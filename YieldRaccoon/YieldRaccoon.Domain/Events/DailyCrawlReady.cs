using System.Diagnostics;

namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Event published when the Rx.NET timer elapses after ~24 hours and the crawler is ready for its next daily run.
/// </summary>
/// <remarks>
/// <para>
/// Published when the scheduled time (from <see cref="DailyCrawlScheduled.ScheduledTime"/>) is reached.
/// This signals that the crawler is ready to start a new session to load all funds.
/// </para>
///
/// <para><strong>Post-actions (Application Layer):</strong></para>
/// <list type="bullet">
///   <item><description>Navigate browser to fund list page</description></item>
///   <item><description>Wait for initial 20 funds to auto-load</description></item>
///   <item><description>Automatically start a crawl session</description></item>
/// </list>
/// </remarks>
[DebuggerDisplay("DailyCrawlReady at {OccurredAt}")]
public sealed record DailyCrawlReady : IDomainEvent
{
    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="DailyCrawlReady"/> event with UTC timestamp.
    /// </summary>
    /// <returns>A new immutable event instance.</returns>
    public static DailyCrawlReady Create()
    {
        return new DailyCrawlReady
        {
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
