namespace PdfTextExtractor.Core.Domain.Events.Page;

/// <summary>
/// Raised when an empty page is detected during extraction.
/// </summary>
public class EmptyPageDetected : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
}
