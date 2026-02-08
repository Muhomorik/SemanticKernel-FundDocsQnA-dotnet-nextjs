using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Immutable snapshot of the current crawl session state for UI binding.
/// </summary>
/// <remarks>
/// <para>
/// This read model is projected from domain events by the orchestrator
/// and emitted via observable stream for presentation layer consumption.
/// </para>
/// <para>
/// The presentation layer should NOT derive this state directly from the event store;
/// instead, it should subscribe to the orchestrator's <c>SessionState</c> observable.
/// </para>
/// </remarks>
[DebuggerDisplay("Session {SessionId?.Value}: Active={IsActive}, Batch={CurrentBatchNumber}/{EstimatedBatchCount}")]
public sealed record CrawlSessionState
{
    /// <summary>
    /// Gets whether a crawl session is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets the unique identifier of the active session, or null if no session is active.
    /// </summary>
    public CrawlSessionId? SessionId { get; init; }

    /// <summary>
    /// Gets the number of batches that have been completed in the current session.
    /// </summary>
    public int CurrentBatchNumber { get; init; }

    /// <summary>
    /// Gets the estimated total number of batches for the session.
    /// </summary>
    public int EstimatedBatchCount { get; init; }

    /// <summary>
    /// Gets the total number of funds loaded so far in the session.
    /// </summary>
    public int FundsLoaded { get; init; }

    /// <summary>
    /// Gets the estimated time remaining until session completion.
    /// </summary>
    public TimeSpan EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Gets whether a delay countdown is currently in progress before the next batch.
    /// </summary>
    public bool IsDelayInProgress { get; init; }

    /// <summary>
    /// Gets a human-readable status message describing the current session state.
    /// </summary>
    public string StatusMessage { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of seconds remaining in the current delay countdown.
    /// </summary>
    /// <remarks>
    /// Only meaningful when <see cref="IsDelayInProgress"/> is true.
    /// </remarks>
    public int DelayCountdown { get; init; }

    /// <summary>
    /// Gets an inactive session state instance.
    /// </summary>
    public static CrawlSessionState Inactive => new()
    {
        IsActive = false,
        SessionId = null,
        CurrentBatchNumber = 0,
        EstimatedBatchCount = 0,
        FundsLoaded = 0,
        EstimatedTimeRemaining = TimeSpan.Zero,
        IsDelayInProgress = false,
        StatusMessage = string.Empty,
        DelayCountdown = 0
    };
}
