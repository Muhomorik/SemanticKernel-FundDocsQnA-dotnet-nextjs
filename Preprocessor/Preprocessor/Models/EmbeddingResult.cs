using System.Text.Json.Serialization;

namespace Preprocessor.Models;

/// <summary>
/// Represents an embedding result for a document chunk, ready for JSON serialization.
/// </summary>
public class EmbeddingResult
{
    /// <summary>
    /// Unique identifier for this embedding (e.g., "document_page1_chunk0").
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// The extracted text content.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>
    /// The embedding vector.
    /// </summary>
    [JsonPropertyName("embedding")]
    public required float[] Embedding { get; init; }

    /// <summary>
    /// The source PDF file name.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// The page number (1-based) from which this text was extracted.
    /// </summary>
    [JsonPropertyName("page")]
    public required int Page { get; init; }
}
