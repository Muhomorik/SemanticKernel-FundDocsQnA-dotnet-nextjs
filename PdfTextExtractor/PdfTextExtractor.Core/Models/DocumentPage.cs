namespace PdfTextExtractor.Core.Models;

/// <summary>
/// Represents a page of text extracted from a PDF document.
/// </summary>
public class DocumentPage
{
    public required string SourceFile { get; init; }
    public int PageNumber { get; init; }
    public required string PageText { get; init; }

    /// <summary>
    /// Number of tokens used in the prompt/input for this page.
    /// Returns 0 if token usage information is not available or not applicable (e.g., PdfPig extraction).
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// Number of tokens generated in the completion/output for this page.
    /// Returns 0 if token usage information is not available or not applicable (e.g., PdfPig extraction).
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// Total number of tokens used (prompt + completion) for this page.
    /// Returns 0 if token usage information is not available or not applicable (e.g., PdfPig extraction).
    /// </summary>
    public int TotalTokens { get; init; }
}
