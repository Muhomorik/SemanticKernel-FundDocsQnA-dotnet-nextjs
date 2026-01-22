namespace PdfTextExtractor.Core.Configuration;

/// <summary>
/// Parameters for OpenAI vision-based OCR text extraction.
/// </summary>
public class OpenAIParameters
{
    public required string PdfFolderPath { get; init; }
    public required string OutputFolderPath { get; init; }
    public required string ApiKey { get; init; }
    public string VisionModelName { get; init; } = "gpt-4o";
    public int RasterizationDpi { get; init; } = 150;
    public int MaxTokens { get; init; } = 2000;
    public string DetailLevel { get; init; } = "high"; // "low", "high", or "auto"
}
