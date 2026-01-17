namespace PdfTextExtractor.Core.Domain.Events.Document;

/// <summary>
/// Raised when extraction starts for a single document.
/// </summary>
public class DocumentExtractionStarted : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public long FileSizeBytes { get; init; }
}
