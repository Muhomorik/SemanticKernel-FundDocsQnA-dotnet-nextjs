using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.FileSystem;

/// <summary>
/// Service for writing text files.
/// </summary>
public interface ITextFileWriter
{
    Task WriteTextFileAsync(string filePath, string content, CancellationToken cancellationToken = default);
    Task WritePagesAsync(string outputFolderPath, string pdfFileName, IEnumerable<DocumentPage> pages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all pages to a single merged text file.
    /// Pages are concatenated directly in page number order to preserve table layouts that may span multiple pages.
    /// </summary>
    /// <returns>The full path to the created merged text file.</returns>
    Task<string> WriteMergedDocumentAsync(
        string outputFolderPath,
        string pdfFileName,
        IEnumerable<DocumentPage> pages,
        CancellationToken cancellationToken = default);
}
