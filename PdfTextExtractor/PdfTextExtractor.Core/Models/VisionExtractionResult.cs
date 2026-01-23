namespace PdfTextExtractor.Core.Models;

/// <summary>
/// Result of a vision-based text extraction operation.
/// </summary>
public record VisionExtractionResult
{
    /// <summary>
    /// Extracted text content from the image.
    /// </summary>
    public required string ExtractedText { get; init; }

    /// <summary>
    /// Number of tokens used in the prompt/input.
    /// Returns 0 if token usage information is not available.
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// Number of tokens generated in the completion/output.
    /// Returns 0 if token usage information is not available.
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// Total number of tokens used (prompt + completion).
    /// Returns 0 if token usage information is not available.
    /// </summary>
    public int TotalTokens { get; init; }
}
