using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.AboutFund;

/// <summary>
/// Event published when a browsing session completes after visiting all scheduled funds.
/// </summary>
[DebuggerDisplay("AboutFundSessionCompleted: Session={SessionId}, FundsVisited={FundsVisited}, Duration={Duration} at {OccurredAt}")]
public sealed record AboutFundSessionCompleted : IAboutFundEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this browsing session.
    /// </summary>
    public required AboutFundSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the total number of funds visited during the session.
    /// </summary>
    public required int FundsVisited { get; init; }

    /// <summary>
    /// Gets the total duration of the browsing session.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="AboutFundSessionCompleted"/> event with UTC timestamp.
    /// </summary>
    public static AboutFundSessionCompleted Create(
        AboutFundSessionId sessionId,
        int fundsVisited,
        DateTimeOffset startedAt)
    {
        var now = DateTimeOffset.UtcNow;
        return new AboutFundSessionCompleted
        {
            SessionId = sessionId,
            FundsVisited = fundsVisited,
            Duration = now - startedAt,
            OccurredAt = now
        };
    }
}
