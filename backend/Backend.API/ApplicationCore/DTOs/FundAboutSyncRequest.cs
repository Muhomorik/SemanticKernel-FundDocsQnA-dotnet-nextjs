namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// Request body for <c>POST /api/funds/about</c>.
/// </summary>
/// <remarks>
/// Mirror of YieldRaccoon.Application.DTOs.Api.FundAboutSyncRequest — same shape, own namespace.
/// </remarks>
public sealed record FundAboutSyncRequest
{
    public required ApiFundDto Profile { get; init; }
    public IReadOnlyList<ApiFundHistoryPointDto> HistoryRecords { get; init; } = [];
}
