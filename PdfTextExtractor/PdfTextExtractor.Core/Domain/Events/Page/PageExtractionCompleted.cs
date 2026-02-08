namespace PdfTextExtractor.Core.Domain.Events.Page;

/// <summary>
/// Raised when extraction completes for a single page.
/// </summary>
public class PageExtractionCompleted : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public int ExtractedTextLength { get; init; }
}
