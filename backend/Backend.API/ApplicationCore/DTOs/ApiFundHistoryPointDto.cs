namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// HTTP API DTO for a single NAV data point from chart history.
/// </summary>
/// <remarks>
/// Mirror of YieldRaccoon.Application.DTOs.Api.ApiFundHistoryPointDto — same shape, own namespace.
/// </remarks>
public sealed record ApiFundHistoryPointDto
{
    public required string Isin { get; init; }
    public decimal? Nav { get; init; }
    public string? NavDate { get; init; }
}
