using System.Text.Json.Serialization;

namespace Preprocessor.Models;

/// <summary>
/// DTO matching Backend.API's EmbeddingDto structure.
/// </summary>
/// <remarks>
/// IMPORTANT: Backend API uses default ASP.NET Core JSON serialization (camelCase).
/// The property names MUST match exactly:
/// - "sourceFile" (lowercase 's') matches Cosmos DB partition key: /sourceFile
/// - Changing this to "SourceFile" (PascalCase) will break Cosmos DB partitioning
/// - All properties use camelCase to match ASP.NET Core defaults
/// </remarks>
internal record BackendEmbeddingDto
{
    /// <summary>
    /// Unique identifier for the embedding (e.g., "document_page1_chunk0").
    /// JSON: "id" (camelCase)
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Text content of the chunk.
    /// JSON: "text" (camelCase)
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>
    /// Vector embedding (1536 dimensions for text-embedding-3-small).
    /// JSON: "embedding" (camelCase)
    /// </summary>
    [JsonPropertyName("embedding")]
    public required float[] Embedding { get; init; }

    /// <summary>
    /// Source PDF filename.
    /// JSON: "sourceFile" (camelCase - CRITICAL: must match Cosmos DB partition key /sourceFile)
    /// </summary>
    [JsonPropertyName("sourceFile")]
    public required string SourceFile { get; init; }

    /// <summary>
    /// Page number in the source PDF.
    /// JSON: "page" (camelCase)
    /// </summary>
    [JsonPropertyName("page")]
    public required int Page { get; init; }
}