namespace PdfTextExtractor.Core.Domain.Events.Batch;

/// <summary>
/// Raised when a batch extraction completes successfully.
/// </summary>
public class BatchExtractionCompleted : PdfExtractionEventBase
{
    public required string[] OutputFilePaths { get; init; }
    public int TotalFilesProcessed { get; init; }
    public TimeSpan TotalDuration { get; init; }
}
