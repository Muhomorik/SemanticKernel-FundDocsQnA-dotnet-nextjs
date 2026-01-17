namespace PdfTextExtractor.Core.Configuration;

/// <summary>
/// Parameters for LM Studio OCR-based text extraction.
/// </summary>
public class LMStudioParameters
{
    public required string PdfFolderPath { get; init; }
    public required string OutputFolderPath { get; init; }
    public string LMStudioUrl { get; init; } = "http://localhost:1234";
    public required string VisionModelName { get; init; }
    public int RasterizationDpi { get; init; } = 300;
    public int ChunkSize { get; init; } = 1000;
}
