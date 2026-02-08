using System.Diagnostics;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Represents a fund scheduled for detail page browsing, with its history record count.
/// </summary>
/// <remarks>
/// Projected from the database query that joins FundProfiles with FundHistoryRecords
/// to determine which funds have the least historical data.
/// </remarks>
[DebuggerDisplay("AboutFundScheduleItem: {Name} ({Isin}), OrderbookId={OrderbookId}, HistoryRecords={HistoryRecordCount}")]
public sealed record AboutFundScheduleItem
{
    /// <summary>
    /// Gets the fund's ISIN identifier.
    /// </summary>
    public required string Isin { get; init; }

    /// <summary>
    /// Gets the fund's OrderbookId used in the external URL.
    /// </summary>
    public required string? OrderbookId { get; init; }

    /// <summary>
    /// Gets the fund's display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the number of history records for this fund.
    /// </summary>
    public required int HistoryRecordCount { get; init; }
}
