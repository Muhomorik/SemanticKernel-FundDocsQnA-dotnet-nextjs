using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.AboutFund;

/// <summary>
/// Event published when navigation to a fund detail page fails.
/// </summary>
[DebuggerDisplay("AboutFundNavigationFailed: Session={SessionId}, Isin={Isin}, Reason={Reason} at {OccurredAt}")]
public sealed record AboutFundNavigationFailed : IAboutFundEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this browsing session.
    /// </summary>
    public required AboutFundSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the ISIN of the fund that failed to load.
    /// </summary>
    public required string Isin { get; init; }

    /// <summary>
    /// Gets the reason for the navigation failure.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="AboutFundNavigationFailed"/> event with UTC timestamp.
    /// </summary>
    public static AboutFundNavigationFailed Create(
        AboutFundSessionId sessionId,
        string isin,
        string reason)
    {
        return new AboutFundNavigationFailed
        {
            SessionId = sessionId,
            Isin = isin,
            Reason = reason,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
