namespace PdfTextExtractor.Core.Models;

/// <summary>
/// Represents a page of text extracted from a PDF document.
/// </summary>
public class DocumentPage
{
    public required string SourceFile { get; init; }
    public int PageNumber { get; init; }
    public required string PageText { get; init; }
}
