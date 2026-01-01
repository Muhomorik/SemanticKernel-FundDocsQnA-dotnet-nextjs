using Backend.API.Domain.ValueObjects;

namespace Backend.API.Domain.Models;

/// <summary>
/// Core domain model representing a searchable document chunk.
/// Encapsulates business rules about document chunks.
/// </summary>
public class DocumentChunk
{
    public string Id { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public EmbeddingVector Vector { get; private set; } = null!;
    public DocumentMetadata Metadata { get; private set; } = null!;

    private DocumentChunk() { }

    /// <summary>
    /// Factory method to create a validated DocumentChunk.
    /// </summary>
    public static DocumentChunk Create(
        string id,
        string text,
        float[] embedding,
        string source,
        int page)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));

        return new DocumentChunk
        {
            Id = id,
            Text = text,
            Vector = new EmbeddingVector(embedding),
            Metadata = new DocumentMetadata(source, page)
        };
    }
}
