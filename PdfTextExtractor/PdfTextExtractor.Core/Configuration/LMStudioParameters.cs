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
    public int MaxTokens { get; init; } = 200;

    /// <summary>
    /// Prompt sent to the vision model for text extraction.
    /// </summary>
    public string ExtractionPrompt { get; init; } = "Extract all text from this image. Return only the text, no explanations.";
}
