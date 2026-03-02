namespace YieldRaccoon.Application.DTOs.Api;

/// <summary>
/// HTTP API DTO for a single NAV data point from chart history.
/// </summary>
/// <remarks>
/// Used in <see cref="FundAboutSyncRequest"/> to send chart data collected from fund detail pages.
/// Each point represents one day's NAV value.
/// </remarks>
public sealed record ApiFundHistoryPointDto
{
    public required string Isin { get; init; }
    public decimal? Nav { get; init; }
    public string? NavDate { get; init; }
}
