namespace YieldRaccoon.Domain.Events.AboutFund;

/// <summary>
/// Marker interface for all about-fund browsing events in the YieldRaccoon system.
/// </summary>
/// <remarks>
/// <para>
/// About-fund events represent facts that have occurred during fund detail page browsing sessions.
/// They are immutable, named in past tense, and include timestamps for ordering.
/// </para>
///
/// <para><strong>Independence from crawl events:</strong></para>
/// <para>
/// This interface is intentionally separate from <see cref="IDomainEvent"/> to maintain
/// bounded context separation between fund list crawling and fund detail browsing.
/// </para>
///
/// <para><strong>Design Principles:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Immutability:</strong> All events are <c>sealed record</c> types with <c>init</c>-only properties.</description></item>
///   <item><description><strong>Past tense naming:</strong> Event names describe what happened (e.g., <c>AboutFundNavigationCompleted</c>).</description></item>
///   <item><description><strong>Timestamps:</strong> All events include <see cref="OccurredAt"/> for chronological ordering.</description></item>
///   <item><description><strong>Correlation IDs:</strong> Events include session IDs for grouping related events.</description></item>
///   <item><description><strong>Static factory methods:</strong> Events are created via <c>Create()</c> methods that set <see cref="OccurredAt"/> to UTC now.</description></item>
/// </list>
/// </remarks>
public interface IAboutFundEvent
{
    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
