namespace PdfTextExtractor.Core.Configuration;

/// <summary>
/// Parameters for PdfPig text extraction.
/// </summary>
public class PdfPigParameters
{
    public required string PdfFolderPath { get; init; }
    public required string OutputFolderPath { get; init; }

    /// <summary>
    /// If true, skip extraction for pages whose text files already exist in the output folder.
    /// Enables incremental/resume extraction. Default is false.
    /// </summary>
    public bool SkipIfExists { get; init; } = false;
}
