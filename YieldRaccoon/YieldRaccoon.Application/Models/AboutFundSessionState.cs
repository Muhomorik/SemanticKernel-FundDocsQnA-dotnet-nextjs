using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Immutable snapshot of the current about-fund browsing session state for UI binding.
/// </summary>
/// <remarks>
/// <para>
/// This read model is projected from about-fund events by the orchestrator
/// and emitted via observable stream for presentation layer consumption.
/// </para>
/// </remarks>
[DebuggerDisplay("AboutFund Session: Active={IsActive}, Index={CurrentIndex}/{TotalFunds}, Fund={CurrentFundName}")]
public sealed record AboutFundSessionState
{
    /// <summary>
    /// Gets whether a browsing session is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets the unique identifier of the active session, or null if no session is active.
    /// </summary>
    public AboutFundSessionId? SessionId { get; init; }

    /// <summary>
    /// Gets the zero-based index of the current fund in the schedule.
    /// </summary>
    public int CurrentIndex { get; init; }

    /// <summary>
    /// Gets the total number of funds in the schedule.
    /// </summary>
    public int TotalFunds { get; init; }

    /// <summary>
    /// Gets the ISIN of the fund currently being viewed.
    /// </summary>
    public string? CurrentIsin { get; init; }

    /// <summary>
    /// Gets the display name of the fund currently being viewed.
    /// </summary>
    public string? CurrentFundName { get; init; }

    /// <summary>
    /// Gets a human-readable status message describing the current session state.
    /// </summary>
    public string StatusMessage { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether a delay countdown is currently in progress before the next fund.
    /// </summary>
    public bool IsDelayInProgress { get; init; }

    /// <summary>
    /// Gets the number of seconds remaining in the current delay countdown.
    /// Only meaningful when <see cref="IsDelayInProgress"/> is true.
    /// </summary>
    public int DelayCountdown { get; init; }

    /// <summary>
    /// Gets the estimated time remaining until session completion.
    /// </summary>
    public TimeSpan EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Gets an inactive session state instance.
    /// </summary>
    public static AboutFundSessionState Inactive => new()
    {
        IsActive = false,
        SessionId = null,
        CurrentIndex = 0,
        TotalFunds = 0,
        CurrentIsin = null,
        CurrentFundName = null,
        StatusMessage = string.Empty,
        IsDelayInProgress = false,
        DelayCountdown = 0,
        EstimatedTimeRemaining = TimeSpan.Zero
    };
}
