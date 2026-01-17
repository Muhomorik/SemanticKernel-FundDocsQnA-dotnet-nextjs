namespace PdfTextExtractor.Core.Domain.Events.Infrastructure;

/// <summary>
/// Raised to report extraction progress updates.
/// </summary>
public class ExtractionProgressUpdated : PdfExtractionEventBase
{
    public required string FilePath { get; init; }
    public double OverallPercentage { get; init; }
    public int PagesProcessed { get; init; }
    public int TotalPages { get; init; }
    public required string CurrentOperation { get; init; }
}
