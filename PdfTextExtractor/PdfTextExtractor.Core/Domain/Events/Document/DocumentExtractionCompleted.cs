namespace PdfTextExtractor.Core.Domain.Events.Document;

/// <summary>
/// Raised when extraction completes for a single document.
/// </summary>
public class DocumentExtractionCompleted : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public int TotalPages { get; init; }
    public int TotalChunks { get; init; }
    public required string OutputFilePath { get; init; }
    public TimeSpan Duration { get; init; }
}
