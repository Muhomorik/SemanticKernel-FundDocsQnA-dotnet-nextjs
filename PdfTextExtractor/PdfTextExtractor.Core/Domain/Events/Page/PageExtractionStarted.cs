namespace PdfTextExtractor.Core.Domain.Events.Page;

/// <summary>
/// Raised when extraction starts for a single page.
/// </summary>
public class PageExtractionStarted : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public int TotalPages { get; init; }
}
