namespace PdfTextExtractor.Core.Models;

/// <summary>
/// Represents a text chunk extracted from a PDF document.
/// </summary>
public class DocumentChunk
{
    public required string SourceFile { get; init; }
    public int PageNumber { get; init; }
    public int ChunkIndex { get; init; }
    public required string Content { get; init; }
}
