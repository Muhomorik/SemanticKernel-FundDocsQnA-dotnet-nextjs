using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.AboutFund;

/// <summary>
/// Event published when a browsing session is cancelled by the user or window close.
/// </summary>
[DebuggerDisplay("AboutFundSessionCancelled: Session={SessionId}, FundsVisited={FundsVisited}, Reason={Reason} at {OccurredAt}")]
public sealed record AboutFundSessionCancelled : IAboutFundEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this browsing session.
    /// </summary>
    public required AboutFundSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the number of funds visited before cancellation.
    /// </summary>
    public required int FundsVisited { get; init; }

    /// <summary>
    /// Gets the reason for cancellation.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="AboutFundSessionCancelled"/> event with UTC timestamp.
    /// </summary>
    public static AboutFundSessionCancelled Create(
        AboutFundSessionId sessionId,
        int fundsVisited,
        string reason)
    {
        return new AboutFundSessionCancelled
        {
            SessionId = sessionId,
            FundsVisited = fundsVisited,
            Reason = reason,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
