namespace PdfTextExtractor.Core.Domain.Events.Ocr;

/// <summary>
/// Raised when OCR processing completes successfully.
/// </summary>
public class OcrProcessingCompleted : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public int ExtractedTextLength { get; init; }
    public TimeSpan ProcessingDuration { get; init; }
}
