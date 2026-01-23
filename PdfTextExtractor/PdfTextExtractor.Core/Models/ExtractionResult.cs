using PdfTextExtractor.Core.Configuration;

namespace PdfTextExtractor.Core.Models;

/// <summary>
/// Result of a text extraction operation.
/// </summary>
public class ExtractionResult
{
    public required string PdfFilePath { get; init; }
    public required Dictionary<int, string> PageTextFiles { get; init; }
    public int TotalPages { get; init; }

    /// <summary>
    /// Number of pages that were skipped because text files already existed.
    /// </summary>
    public int SkippedPages { get; init; }

    /// <summary>
    /// Number of pages that were actually extracted (not skipped).
    /// </summary>
    public int ExtractedPages { get; init; }

    public TimeSpan Duration { get; init; }
    public TextExtractionMethod Method { get; init; }

    /// <summary>
    /// Total number of tokens used in prompts/inputs across all pages.
    /// Returns 0 if token usage information is not available or not applicable (e.g., PdfPig extraction).
    /// </summary>
    public int TotalPromptTokens { get; init; }

    /// <summary>
    /// Total number of tokens generated in completions/outputs across all pages.
    /// Returns 0 if token usage information is not available or not applicable (e.g., PdfPig extraction).
    /// </summary>
    public int TotalCompletionTokens { get; init; }

    /// <summary>
    /// Total number of tokens used (prompt + completion) across all pages.
    /// Returns 0 if token usage information is not available or not applicable (e.g., PdfPig extraction).
    /// </summary>
    public int TotalTokens { get; init; }
}
