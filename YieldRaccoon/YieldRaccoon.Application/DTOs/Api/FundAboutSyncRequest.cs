namespace YieldRaccoon.Application.DTOs.Api;

/// <summary>
/// Request body for <c>POST /api/funds/about</c>.
/// </summary>
/// <remarks>
/// Sent after visiting a fund detail page (auto or manual): contains the fund profile
/// and chart history records spanning multiple time periods.
/// History records are insert-only — the Backend skips existing (ISIN, NavDate) pairs.
/// </remarks>
public sealed record FundAboutSyncRequest
{
    public required ApiFundDto Profile { get; init; }
    public IReadOnlyList<ApiFundHistoryPointDto> HistoryRecords { get; init; } = [];
}
