namespace YieldRaccoon.Domain.Events;

/// <summary>
/// Marker interface for all domain events in the YieldRaccoon system.
/// </summary>
/// <remarks>
/// <para>
/// Domain events represent facts that have occurred in the domain.
/// They are immutable, named in past tense, and include timestamps for ordering.
/// </para>
///
/// <para><strong>Design Principles:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Immutability:</strong> All events are <c>sealed record</c> types with <c>init</c>-only properties.</description></item>
///   <item><description><strong>Past tense naming:</strong> Event names describe what happened (e.g., <c>CrawlSessionStarted</c>, not <c>StartCrawlSession</c>).</description></item>
///   <item><description><strong>Timestamps:</strong> All events include <see cref="OccurredAt"/> for chronological ordering.</description></item>
///   <item><description><strong>Correlation IDs:</strong> Events include session IDs for grouping related events.</description></item>
///   <item><description><strong>Static factory methods:</strong> Events are created via <c>Create()</c> methods that set <see cref="OccurredAt"/> to UTC now.</description></item>
/// </list>
///
/// <para><strong>Event Publishing:</strong></para>
/// <para>
/// Domain events are NOT published by entities. Instead, Application layer services
/// publish events to the event store after successful operations. This maintains
/// layer separation and ensures events only represent committed state changes.
/// </para>
///
/// <para><strong>Event Store Pattern:</strong></para>
/// <para>
/// Events are stored in an in-memory sorted collection (<c>List&lt;IDomainEvent&gt;</c>)
/// ordered by <see cref="OccurredAt"/>. LINQ queries enable progress calculation,
/// remaining time estimation, and session history reconstruction.
/// </para>
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    /// <remarks>
    /// Used for chronological ordering in the event store and progress tracking.
    /// Set automatically by static factory methods to <see cref="DateTimeOffset.UtcNow"/>.
    /// </remarks>
    DateTimeOffset OccurredAt { get; }
}
