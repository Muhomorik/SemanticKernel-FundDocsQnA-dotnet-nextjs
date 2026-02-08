using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.AboutFund;

/// <summary>
/// Event published when the browser successfully loads a fund detail page.
/// </summary>
[DebuggerDisplay("AboutFundNavigationCompleted: Session={SessionId}, Isin={Isin}, Index={Index} at {OccurredAt}")]
public sealed record AboutFundNavigationCompleted : IAboutFundEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this browsing session.
    /// </summary>
    public required AboutFundSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the ISIN of the fund that was loaded.
    /// </summary>
    public required string Isin { get; init; }

    /// <summary>
    /// Gets the OrderbookId used in the URL.
    /// </summary>
    public required string OrderbookId { get; init; }

    /// <summary>
    /// Gets the zero-based index of this fund in the browsing schedule.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="AboutFundNavigationCompleted"/> event with UTC timestamp.
    /// </summary>
    public static AboutFundNavigationCompleted Create(
        AboutFundSessionId sessionId,
        string isin,
        string orderbookId,
        int index)
    {
        return new AboutFundNavigationCompleted
        {
            SessionId = sessionId,
            Isin = isin,
            OrderbookId = orderbookId,
            Index = index,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
