using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Backend.API.Infrastructure.Persistence.Models;

/// <summary>
/// DTO for Cosmos DB document with vector embedding.
/// Matches Cosmos DB schema with partition key /sourceFile.
/// Uses both System.Text.Json and Newtonsoft.Json attributes for compatibility.
/// </summary>
public class CosmosDbDocumentDto
{
    /// <summary>
    /// Unique identifier for the document chunk.
    /// </summary>
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Source PDF filename (partition key).
    /// </summary>
    [JsonProperty("sourceFile")]
    [JsonPropertyName("sourceFile")]
    public required string SourceFile { get; init; }

    /// <summary>
    /// Page number in the source PDF.
    /// </summary>
    [JsonProperty("page")]
    [JsonPropertyName("page")]
    public required int Page { get; init; }

    /// <summary>
    /// Text content of the chunk.
    /// </summary>
    [JsonProperty("text")]
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>
    /// Vector embedding (1536 dimensions for text-embedding-3-small).
    /// </summary>
    [JsonProperty("embedding")]
    [JsonPropertyName("embedding")]
    public required float[] Embedding { get; init; }
}
