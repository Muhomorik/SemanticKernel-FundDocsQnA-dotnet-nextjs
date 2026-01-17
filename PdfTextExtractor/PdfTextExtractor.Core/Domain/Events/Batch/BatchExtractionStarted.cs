namespace PdfTextExtractor.Core.Domain.Events.Batch;

/// <summary>
/// Raised when a batch extraction starts.
/// </summary>
public class BatchExtractionStarted : PdfExtractionEventBase
{
    public required string[] FilePaths { get; init; }
    public int TotalFiles { get; init; }
}
