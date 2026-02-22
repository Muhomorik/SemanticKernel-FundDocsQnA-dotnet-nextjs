using System.Text.Json.Serialization;

namespace YieldRaccoon.Infrastructure.Models;

/// <summary>
/// Mirrors the external chart API response shape.
/// Anti-corruption layer model — not a domain concept.
/// </summary>
/// <remarks>
/// <see cref="DataSerie"/> uses <see cref="ResilientDataSerieConverter"/> to handle
/// malformed data points gracefully. The external API occasionally returns <c>y</c> as
/// an object instead of a number — without the converter, a single bad entry would
/// cause <see cref="System.Text.Json.JsonSerializer"/> to throw and lose the entire array.
/// The converter deserializes each element individually and silently skips failures.
/// </remarks>
public sealed record AboutFundChartResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("dataSerie")]
    [property: JsonConverter(typeof(ResilientDataSerieConverter))]
    IReadOnlyList<AboutFundChartDataPoint>? DataSerie);
