using System.Text.Json.Serialization;

namespace YieldRaccoon.Infrastructure.Models;


/// <summary>
/// Anti-corruption DTO for the <c>_api/fund-reference/portfolio-data/{orderBookId}</c> response.
/// </summary>
/// <remarks>
/// Only the country and sector composition arrays are mapped. <c>holdingChartData</c>,
/// <c>previousY</c>, <c>portfolioDate</c>, and <c>previousPortfolioDate</c> are intentionally
/// omitted — System.Text.Json silently ignores unknown properties.
/// </remarks>
public sealed record PortfolioDataResponse(
    [property: JsonPropertyName("countryChartData")] IReadOnlyList<CountryChartDataItem>? CountryChartData,
    [property: JsonPropertyName("sectorChartData")] IReadOnlyList<SectorChartDataItem>? SectorChartData);

/// <summary>
/// Single country allocation entry from the source payload.
/// </summary>
/// <remarks>
/// JSON binding renames the wire field <c>name</c> to <see cref="DisplayName"/> and
/// <c>y</c> to <see cref="Percentage"/> (we don't control the Avanza wire format).
/// </remarks>
public sealed record CountryChartDataItem(
    [property: JsonPropertyName("name")] string DisplayName,
    [property: JsonPropertyName("y")] double Percentage,
    [property: JsonPropertyName("countryCode")] string? CountryCode);

/// <summary>
/// Single sector allocation entry from the source payload.
/// </summary>
public sealed record SectorChartDataItem(
    [property: JsonPropertyName("name")] string DisplayName,
    [property: JsonPropertyName("y")] double Percentage);
