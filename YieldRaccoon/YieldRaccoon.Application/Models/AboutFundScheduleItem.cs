using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Represents a fund scheduled for detail page browsing, with its history record count.
/// </summary>
/// <remarks>
/// Projected from the database query that joins FundProfiles with FundHistoryRecords
/// to determine which funds have the least historical data.
/// Only includes funds that have a valid <see cref="OrderBookId"/> — funds without one
/// are filtered out at the repository level since they cannot be browsed.
/// </remarks>
[DebuggerDisplay("AboutFundScheduleItem: {Name} ({Isin}), OrderbookId={OrderBookId}, HistoryRecords={HistoryRecordCount}, LastVisited={LastVisitedAt}")]
public sealed record AboutFundScheduleItem
{
    /// <summary>
    /// Gets the fund's ISIN identifier.
    /// </summary>
    public required string Isin { get; init; }

    /// <summary>
    /// Gets the fund's OrderBookId used in the external URL.
    /// </summary>
    public required OrderBookId OrderBookId { get; init; }

    /// <summary>
    /// Gets the fund's display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the number of history records for this fund.
    /// </summary>
    public required int HistoryRecordCount { get; init; }

    /// <summary>
    /// Gets the timestamp when this fund was last visited by the about-fund orchestrator,
    /// or null if never visited.
    /// </summary>
    public DateTimeOffset? LastVisitedAt { get; init; }
}
