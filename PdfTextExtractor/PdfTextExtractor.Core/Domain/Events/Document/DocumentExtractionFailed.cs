namespace PdfTextExtractor.Core.Domain.Events.Document;

/// <summary>
/// Raised when extraction fails for a single document.
/// </summary>
public class DocumentExtractionFailed : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public required string ErrorMessage { get; init; }
    public required string ExceptionType { get; init; }
    public int? PageNumberWhereFailed { get; init; }
}
