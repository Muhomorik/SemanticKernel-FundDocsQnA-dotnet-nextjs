namespace YieldRaccoon.Application.DTOs.Api;

/// <summary>
/// Response from <c>POST /api/funds/list</c> and <c>POST /api/funds/about</c>.
/// </summary>
public sealed record FundSyncResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public int ProfilesProcessed { get; init; }
    public int HistoryRecordsInserted { get; init; }
}
