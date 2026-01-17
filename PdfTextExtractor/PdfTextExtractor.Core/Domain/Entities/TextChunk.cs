using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Domain.Entities;

/// <summary>
/// Text chunk entity with unique identity.
/// </summary>
public class TextChunk
{
    public Guid ChunkId { get; private set; }
    public PageNumber PageNumber { get; private set; }
    public int ChunkIndex { get; private set; }
    public ChunkContent Content { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TextChunk() { } // EF Core constructor

    public static TextChunk Create(PageNumber pageNumber, int chunkIndex, ChunkContent content)
    {
        return new TextChunk
        {
            ChunkId = Guid.NewGuid(),
            PageNumber = pageNumber,
            ChunkIndex = chunkIndex,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
