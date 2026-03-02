namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// Request body for <c>POST /api/funds/list</c>.
/// </summary>
/// <remarks>
/// Mirror of YieldRaccoon.Application.DTOs.Api.FundListSyncRequest — same shape, own namespace.
/// </remarks>
public sealed record FundListSyncRequest
{
    public required IReadOnlyList<ApiFundDto> Funds { get; init; }
}
