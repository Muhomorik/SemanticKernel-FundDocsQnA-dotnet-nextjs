using System.Collections.Generic;
using PdfTextExtractor.Core.Configuration;

namespace PdfTextExtractor.Core.Models;

/// <summary>
/// Result of a text extraction operation.
/// </summary>
public class ExtractionResult
{
    public required string PdfFilePath { get; init; }

    /// <summary>
    /// Dictionary mapping page numbers to their individual text file paths.
    /// File naming pattern: {PdfFileNameWithoutExtension}_page_{PageNumber}.txt
    /// </summary>
    /// <remarks>
    /// <para>Page numbers are 1-indexed (first page is 1, not 0).</para>
    /// <para>
    /// Example: For PDF "invoice.pdf" with 3 pages, the dictionary contains:
    /// <list type="bullet">
    /// <item><description>Key: 1, Value: "C:\output\invoice_page_1.txt"</description></item>
    /// <item><description>Key: 2, Value: "C:\output\invoice_page_2.txt"</description></item>
    /// <item><description>Key: 3, Value: "C:\output\invoice_page_3.txt"</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public required IReadOnlyDictionary<int, string> PageTextFiles { get; init; }

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
