namespace Preprocessor;

/// <summary>
/// Configuration options for PDF processing (data only, no behavior).
/// </summary>
public class ProcessingOptions
{
    /// <summary>
    /// Input directory containing PDF files and their corresponding text files.
    /// Text files must follow naming pattern: {PdfBaseName}_page_{PageNumber}.txt
    /// </summary>
    public required string InputDirectory { get; init; }
}
