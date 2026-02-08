namespace PdfTextExtractor.Core.Domain.Events.Ocr;

/// <summary>
/// Raised when page rasterization (PDF to image conversion) starts.
/// </summary>
public class PageRasterizationStarted : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public int TargetDpi { get; init; }
}
