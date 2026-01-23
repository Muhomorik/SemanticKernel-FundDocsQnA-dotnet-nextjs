namespace PdfTextExtractor.Core.Domain.Events.Page;

/// <summary>
/// Raised when page extraction is skipped because text file already exists.
/// </summary>
public class PageExtractionSkipped : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PageNumber { get; init; }
    public required string ExistingTextFilePath { get; init; }
    public int TextLength { get; init; }
}
