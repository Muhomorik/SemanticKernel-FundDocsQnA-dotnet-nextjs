using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.AboutFund;

/// <summary>
/// Event published when a new about-fund browsing session begins.
/// </summary>
/// <remarks>
/// <para>
/// A browsing session represents the workflow of navigating through fund detail pages
/// for funds with the lowest history record counts.
/// </para>
/// </remarks>
[DebuggerDisplay("AboutFundSessionStarted: Session={SessionId}, TotalFunds={TotalFunds}, FirstOrderbookId={FirstOrderbookId} at {OccurredAt}")]
public sealed record AboutFundSessionStarted : IAboutFundEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this browsing session.
    /// </summary>
    public required AboutFundSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the total number of funds scheduled for browsing.
    /// </summary>
    public required int TotalFunds { get; init; }

    /// <summary>
    /// Gets the OrderbookId of the first fund to be browsed.
    /// </summary>
    public required string FirstOrderbookId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="AboutFundSessionStarted"/> event with UTC timestamp.
    /// </summary>
    public static AboutFundSessionStarted Create(
        AboutFundSessionId sessionId,
        int totalFunds,
        string firstOrderbookId)
    {
        return new AboutFundSessionStarted
        {
            SessionId = sessionId,
            TotalFunds = totalFunds,
            FirstOrderbookId = firstOrderbookId,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
