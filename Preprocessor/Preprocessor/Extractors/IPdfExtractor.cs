using Preprocessor.Models;

namespace Preprocessor.Extractors;

/// <summary>
/// Interface for PDF text extraction strategies.
/// </summary>
/// <remarks>
/// REQUIREMENT: All implementations MUST preserve original text formatting including:
/// - Paragraph breaks and line breaks
/// - Indentation and whitespace structure
/// - Tables and structured content
/// - All other formatting elements
/// Implementations should NOT apply text normalization, cleaning, or whitespace collapsing.
/// </remarks>
public interface IPdfExtractor
{
    /// <summary>
    /// The name of the extraction method (e.g., "pdfpig").
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// Extracts text chunks from a PDF file with preserved formatting.
    /// </summary>
    /// <param name="filePath">Path to the PDF file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of document chunks with original text formatting preserved.</returns>
    /// <remarks>
    /// Implementations must preserve all original formatting (line breaks, paragraph structure,
    /// tables, indentation) without applying text normalization or whitespace cleaning.
    /// </remarks>
    Task<IEnumerable<DocumentChunk>> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}
