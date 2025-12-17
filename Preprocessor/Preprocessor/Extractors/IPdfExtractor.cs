using Preprocessor.Models;

namespace Preprocessor.Extractors;

/// <summary>
/// Interface for PDF text extraction strategies.
/// </summary>
public interface IPdfExtractor
{
    /// <summary>
    /// The name of the extraction method (e.g., "pdfpig", "ollama-vision").
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// Extracts text chunks from a PDF file.
    /// </summary>
    /// <param name="filePath">Path to the PDF file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of document chunks extracted from the PDF.</returns>
    Task<IEnumerable<DocumentChunk>> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}
