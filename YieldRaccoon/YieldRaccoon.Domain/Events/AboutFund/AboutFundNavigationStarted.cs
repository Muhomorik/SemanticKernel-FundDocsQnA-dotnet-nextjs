using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Domain.Events.AboutFund;

/// <summary>
/// Event published when the browser begins navigating to a fund detail page.
/// </summary>
[DebuggerDisplay("AboutFundNavigationStarted: Session={SessionId}, Isin={Isin}, Index={Index}, Url={Url} at {OccurredAt}")]
public sealed record AboutFundNavigationStarted : IAboutFundEvent
{
    /// <summary>
    /// Gets the unique correlation ID for this browsing session.
    /// </summary>
    public required AboutFundSessionId SessionId { get; init; }

    /// <summary>
    /// Gets the ISIN of the fund being navigated to.
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
    /// Gets the full URL being navigated to.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Creates a new <see cref="AboutFundNavigationStarted"/> event with UTC timestamp.
    /// </summary>
    public static AboutFundNavigationStarted Create(
        AboutFundSessionId sessionId,
        string isin,
        string orderbookId,
        int index,
        string url)
    {
        return new AboutFundNavigationStarted
        {
            SessionId = sessionId,
            Isin = isin,
            OrderbookId = orderbookId,
            Index = index,
            Url = url,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
