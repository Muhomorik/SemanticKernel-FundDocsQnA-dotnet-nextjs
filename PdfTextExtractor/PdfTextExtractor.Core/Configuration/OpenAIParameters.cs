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
    public string DetailLevel { get; init; } = "high"; // "low", "high", or "auto"

    /// <summary>
    /// Maximum number of tokens the model can generate in the completion/output for each page.
    /// GPT-4o supports up to 16,384 max output tokens, but defaults to 4,096 if not specified.
    /// Set higher values (e.g., 2000-4000) for dense/complex documents to avoid truncated text.
    /// Lower values reduce API costs but may cut off text mid-extraction.
    /// See: https://platform.openai.com/docs/api-reference/chat
    /// </summary>
    public int MaxTokens { get; init; } = 2000;

    /// <summary>
    /// Prompt sent to the vision model for text extraction.
    /// </summary>
    public string ExtractionPrompt { get; init; } = "Extract all text from this page, excluding headers and footers. Preserve line breaks and paragraph structure. Use markdown formatting (# for headings, ** for bold, etc.) but do not wrap the output in code blocks or add markdown tags. Output only the extracted text with no explanations or commentary.";

    /// <summary>
    /// If true, skip extraction for pages whose text files already exist in the output folder.
    /// Enables incremental/resume extraction. Default is false.
    /// </summary>
    public bool SkipIfExists { get; init; } = false;
}
