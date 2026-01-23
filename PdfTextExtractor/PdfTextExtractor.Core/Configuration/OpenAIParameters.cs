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

    /// <summary>
    /// Prompt sent to the vision model for text extraction.
    /// </summary>
    public string ExtractionPrompt { get; init; } = "Extract all visible text from this image. Return only the text content, preserving formatting and structure. Do not add explanations or commentary.";
}
