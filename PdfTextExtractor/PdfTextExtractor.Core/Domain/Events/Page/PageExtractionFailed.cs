namespace PdfTextExtractor.Core.Domain.Events.Page;

/// <summary>
/// Raised when extraction fails for a single page.
/// </summary>
public class PageExtractionFailed : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public required string ErrorMessage { get; init; }
    public required string ExceptionType { get; init; }
}
