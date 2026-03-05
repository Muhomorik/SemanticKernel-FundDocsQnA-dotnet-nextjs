namespace YieldRaccoon.Application.DTOs.Api;

/// <summary>
/// Request body for <c>POST /api/funds/list</c>.
/// </summary>
/// <remarks>
/// Sent after a crawl session batch: contains all funds from the fund list page,
/// each with a full profile and one daily snapshot.
/// </remarks>
public sealed record FundListSyncRequest
{
    public required IReadOnlyList<ApiFundDto> Funds { get; init; }
}
