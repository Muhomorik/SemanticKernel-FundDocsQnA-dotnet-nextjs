using System.Text.Json.Serialization;

namespace YieldRaccoon.Infrastructure.Models;

/// <summary>
/// A single NAV data point from the chart time series.
/// Anti-corruption layer model — not a domain concept.
/// </summary>
/// <remarks>
/// <para>
/// The external chart API normally returns <c>y</c> as a plain number (e.g. <c>457.83</c>),
/// but occasionally emits a malformed object instead:
/// </para>
/// <code>
/// { "x": 1770678000000, "y": { "source": "465.0", "parsedValue": 465 } }
/// </code>
/// <para>
/// <see cref="System.Text.Json.JsonSerializer"/> cannot deserialize an object token into
/// <see cref="decimal"/>, so a single malformed entry would throw a
/// <see cref="System.Text.Json.JsonException"/> and kill the entire <c>dataSerie</c> array.
/// </para>
/// <para>
/// To mitigate this, <see cref="AboutFundChartResponse.DataSerie"/> uses
/// <see cref="ResilientDataSerieConverter"/> which deserializes elements individually
/// and silently skips any that fail.
/// </para>
/// </remarks>
/// <param name="X">Unix timestamp in milliseconds (midnight CET/CEST).</param>
/// <param name="Y">NAV price value.</param>
public sealed record AboutFundChartDataPoint(
    [property: JsonPropertyName("x")] long X,
    [property: JsonPropertyName("y")] decimal Y);
