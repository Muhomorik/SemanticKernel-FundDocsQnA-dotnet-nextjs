using System.Text.Json.Serialization;

namespace Backend.API.Models;

/// <summary>
/// Represents a document chunk with its embedding vector.
/// This format matches the output from the Preprocessor.
/// </summary>
public class EmbeddingRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for this chunk.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the text content of the chunk.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>
    /// Gets or sets the embedding vector for this chunk.
    /// </summary>
    [JsonPropertyName("embedding")]
    public required float[] Embedding { get; init; }

    /// <summary>
    /// Gets or sets the source PDF filename.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// Gets or sets the page number in the source PDF (1-based).
    /// </summary>
    [JsonPropertyName("page")]
    public required int Page { get; init; }
}
