namespace PdfTextExtractor.Core.Domain.Events.Infrastructure;

/// <summary>
/// Raised when temporary files are cleaned up after processing.
/// </summary>
public class TempFilesCleanedUp : PdfExtractionEventBase
{
    public required string[] DeletedFilePaths { get; init; }
    public int TotalFilesDeleted { get; init; }
}
