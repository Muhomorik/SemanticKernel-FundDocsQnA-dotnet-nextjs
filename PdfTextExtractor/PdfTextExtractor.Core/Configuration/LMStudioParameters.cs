namespace PdfTextExtractor.Core.Configuration;

/// <summary>
/// Parameters for LM Studio OCR-based text extraction.
/// </summary>
public class LMStudioParameters
{
    public required string PdfFolderPath { get; init; }
    public required string OutputFolderPath { get; init; }
    public string LMStudioUrl { get; init; } = "http://localhost:1234";
    public string VisionModelName { get; init; } = "qwen/qwen2.5-vl-7b";
    public int RasterizationDpi { get; init; } = 150;
    public int ChunkSize { get; init; } = 1000;
    public int MaxTokens { get; init; } = 200;
}
