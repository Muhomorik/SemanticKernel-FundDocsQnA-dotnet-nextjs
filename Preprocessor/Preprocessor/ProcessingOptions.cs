namespace Preprocessor;

/// <summary>
/// Configuration options for PDF processing (data only, no behavior).
/// </summary>
public class ProcessingOptions
{
    /// <summary>
    /// Extraction method (e.g., "pdfpig").
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Input directory containing PDF files.
    /// </summary>
    public required string InputDirectory { get; init; }
}
