namespace PdfTextExtractor.Core.Domain.Events.Document;

/// <summary>
/// Raised when extraction is cancelled for a single document.
/// </summary>
public class DocumentExtractionCancelled : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int PagesProcessedBeforeCancellation { get; init; }
}
