namespace PdfTextExtractor.Core.Domain.Events.TextProcessing;

/// <summary>
/// Raised when text is chunked into smaller segments.
/// </summary>
public class TextChunked : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public int ChunkCount { get; init; }
    public required int[] ChunkSizes { get; init; }
}
