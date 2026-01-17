namespace PdfTextExtractor.Core.Domain.Events.TextProcessing;

/// <summary>
/// Raised when an individual chunk is created.
/// </summary>
public class ChunkCreated : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public int ChunkIndex { get; init; }
    public int ContentLength { get; init; }
    public required string ContentPreview { get; init; }
}
