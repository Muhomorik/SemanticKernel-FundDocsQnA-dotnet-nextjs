namespace PdfTextExtractor.Core.Domain.Events.Infrastructure;

/// <summary>
/// Raised when a temporary image file is saved during OCR processing.
/// </summary>
public class TempImageSaved : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public required string TempImagePath { get; init; }
    public long ImageSizeBytes { get; init; }
}
