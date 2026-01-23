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

    /// <summary>
    /// Maximum number of tokens the model can generate for each page prediction.
    /// Controls the maximum length of the extracted text output. Set to -1 for unlimited generation.
    /// Lower values (e.g., 200-500) work for simple documents and reduce processing time.
    /// Higher values (e.g., 1000-2000) needed for dense/complex documents to avoid truncation.
    /// When this limit is reached, the prediction stops with reason 'maxPredictedTokensReached'.
    /// See: https://lmstudio.ai/docs/typescript/api-reference/llm-prediction-config-input
    /// </summary>
    public int MaxTokens { get; init; } = 200;

    /// <summary>
    /// Prompt sent to the vision model for text extraction.
    /// </summary>
    public string ExtractionPrompt { get; init; } = "Extract all text from this image. Return only the text, no explanations.";

    /// <summary>
    /// If true, skip extraction for pages whose text files already exist in the output folder.
    /// Enables incremental/resume extraction. Default is false.
    /// </summary>
    public bool SkipIfExists { get; init; } = false;
}
