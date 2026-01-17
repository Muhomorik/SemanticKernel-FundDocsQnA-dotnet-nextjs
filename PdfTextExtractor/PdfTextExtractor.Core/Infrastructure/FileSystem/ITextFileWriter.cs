using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.FileSystem;

/// <summary>
/// Service for writing text files.
/// </summary>
public interface ITextFileWriter
{
    Task WriteTextFileAsync(string filePath, string content, CancellationToken cancellationToken = default);
    Task WriteChunksAsync(string filePath, IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
}
