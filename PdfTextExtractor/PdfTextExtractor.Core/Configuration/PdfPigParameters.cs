namespace PdfTextExtractor.Core.Configuration;

/// <summary>
/// Parameters for PdfPig text extraction.
/// </summary>
public class PdfPigParameters
{
    public required string PdfFolderPath { get; init; }
    public required string OutputFolderPath { get; init; }
}
