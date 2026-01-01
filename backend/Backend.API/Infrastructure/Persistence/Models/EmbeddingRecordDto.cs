using System.Text.Json.Serialization;

namespace Backend.API.Infrastructure.Persistence.Models;

/// <summary>
/// Persistence DTO for JSON serialization.
/// Matches the format from Preprocessor output.
/// </summary>
public class EmbeddingRecordDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("embedding")]
    public required float[] Embedding { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("page")]
    public required int Page { get; init; }
}
