namespace PdfTextExtractor.Core.Domain.Events.Ocr;

/// <summary>
/// Raised when OCR processing starts (sending image to vision model).
/// </summary>
public class OcrProcessingStarted : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public required string VisionModelName { get; init; }
}
