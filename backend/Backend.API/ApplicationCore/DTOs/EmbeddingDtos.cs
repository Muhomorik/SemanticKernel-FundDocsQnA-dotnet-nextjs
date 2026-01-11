using System.ComponentModel.DataAnnotations;

namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// DTO representing a single document chunk embedding.
/// Used for transferring embedding data between Preprocessor and Backend API.
/// </summary>
public record EmbeddingDto
{
    /// <summary>
    /// Unique identifier for the embedding.
    /// </summary>
    [Required(ErrorMessage = "Id is required")]
    public required string Id { get; init; }

    /// <summary>
    /// Text content of the chunk.
    /// </summary>
    [Required(ErrorMessage = "Text is required")]
    [MinLength(1, ErrorMessage = "Text must not be empty")]
    public required string Text { get; init; }

    /// <summary>
    /// Vector embedding (1536 dimensions for text-embedding-3-small).
    /// </summary>
    [Required(ErrorMessage = "Embedding is required")]
    [MinLength(1536, ErrorMessage = "Embedding must contain 1536 dimensions")]
    [MaxLength(1536, ErrorMessage = "Embedding must contain 1536 dimensions")]
    public required float[] Embedding { get; init; }

    /// <summary>
    /// Source PDF filename.
    /// </summary>
    [Required(ErrorMessage = "SourceFile is required")]
    public required string SourceFile { get; init; }

    /// <summary>
    /// Page number in the source PDF.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public required int Page { get; init; }
}

/// <summary>
/// Request to add new embeddings to the vector store.
/// </summary>
public record AddEmbeddingsRequest
{
    /// <summary>
    /// List of embeddings to add.
    /// </summary>
    [Required(ErrorMessage = "Embeddings are required")]
    [MinLength(1, ErrorMessage = "At least one embedding is required")]
    public required IReadOnlyList<EmbeddingDto> Embeddings { get; init; }
}

/// <summary>
/// Request to replace all embeddings in the vector store.
/// WARNING: This is a destructive operation that deletes all existing embeddings.
/// </summary>
public record ReplaceAllEmbeddingsRequest
{
    /// <summary>
    /// New set of embeddings to store (replaces all existing data).
    /// </summary>
    [Required(ErrorMessage = "Embeddings are required")]
    [MinLength(1, ErrorMessage = "At least one embedding is required")]
    public required IReadOnlyList<EmbeddingDto> Embeddings { get; init; }
}

/// <summary>
/// Response for embedding operations.
/// </summary>
public record EmbeddingOperationResponse
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Human-readable message describing the operation result.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Number of embeddings affected by the operation.
    /// </summary>
    public int? Count { get; init; }
}
