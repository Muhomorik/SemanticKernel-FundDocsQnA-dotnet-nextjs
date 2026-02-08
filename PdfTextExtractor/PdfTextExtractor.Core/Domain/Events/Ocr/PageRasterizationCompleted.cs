namespace PdfTextExtractor.Core.Domain.Events.Ocr;

/// <summary>
/// Raised when page rasterization completes.
/// </summary>
public class PageRasterizationCompleted : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public required string TempImagePath { get; init; }
    public long ImageSizeBytes { get; init; }
}
