using System.Text.Json;
using System.Text.Json.Serialization;

namespace YieldRaccoon.Infrastructure.Models;

/// <summary>
/// Deserializes a JSON array of chart data points, silently skipping any element
/// that cannot be parsed as a valid <see cref="AboutFundChartDataPoint"/>.
/// </summary>
/// <remarks>
/// <para>
/// The external chart API occasionally returns malformed <c>y</c> values
/// (e.g. an object <c>{"source": "465.0", "parsedValue": 465}</c> instead of a number).
/// Standard <see cref="JsonSerializer"/> deserialization would throw a
/// <see cref="JsonException"/> for the entire array, losing all valid data points.
/// </para>
/// <para>
/// This converter iterates the array element-by-element, snapshots the
/// <see cref="Utf8JsonReader"/> before each attempt, and on failure restores the
/// snapshot and calls <see cref="Utf8JsonReader.TrySkip"/> to advance past the
/// malformed element. Only successfully parsed points are included in the result.
/// </para>
/// </remarks>
public sealed class ResilientDataSerieConverter : JsonConverter<IReadOnlyList<AboutFundChartDataPoint>>
{
    /// <inheritdoc />
    public override IReadOnlyList<AboutFundChartDataPoint>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for dataSerie");

        var results = new List<AboutFundChartDataPoint>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return results;

            // Snapshot the reader so we can recover on failure.
            // Utf8JsonReader is a value-type ref struct — assignment creates a genuine copy.
            var checkpoint = reader;

            try
            {
                var point = JsonSerializer.Deserialize<AboutFundChartDataPoint>(ref reader, options);
                if (point is not null)
                    results.Add(point);
            }
            catch (JsonException)
            {
                // Restore to start of the malformed element and skip past it entirely.
                reader = checkpoint;
                reader.TrySkip();
            }
        }

        throw new JsonException("Unexpected end of JSON while reading dataSerie array");
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<AboutFundChartDataPoint> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
