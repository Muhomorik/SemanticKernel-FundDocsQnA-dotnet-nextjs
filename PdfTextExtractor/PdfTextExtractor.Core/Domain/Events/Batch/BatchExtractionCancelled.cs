namespace PdfTextExtractor.Core.Domain.Events.Batch;

/// <summary>
/// Raised when a batch extraction is cancelled.
/// </summary>
public class BatchExtractionCancelled : PdfExtractionEventBase
{
    public required string Reason { get; init; }
    public int FilesProcessedBeforeCancellation { get; init; }
}
