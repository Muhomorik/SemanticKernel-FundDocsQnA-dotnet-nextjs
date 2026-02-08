namespace PdfTextExtractor.Core.Domain.Events.Batch;

/// <summary>
/// Raised when a batch extraction fails.
/// </summary>
public class BatchExtractionFailed : PdfExtractionEventBase
{
    public required string ErrorMessage { get; init; }
    public required string ExceptionType { get; init; }
    public int FilesProcessedBeforeFailure { get; init; }
}
