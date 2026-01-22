using PdfTextExtractor.Core.Configuration;

namespace PdfTextExtractor.Core.Models;

/// <summary>
/// Result of a text extraction operation.
/// </summary>
public class ExtractionResult
{
    public required string PdfFilePath { get; init; }
    public required Dictionary<int, string> PageTextFiles { get; init; }
    public int TotalPages { get; init; }
    public TimeSpan Duration { get; init; }
    public TextExtractionMethod Method { get; init; }
}
