namespace PdfTextExtractor.Core.Domain.Events.Ocr;

/// <summary>
/// Raised when OCR processing fails.
/// </summary>
public class OcrProcessingFailed : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public required string ErrorMessage { get; init; }
    public required string ExceptionType { get; init; }
}
