using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.FileSystem;

/// <summary>
/// Implementation of text file writing operations.
/// </summary>
public class TextFileWriter : ITextFileWriter
{
    public async Task WriteTextFileAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }

    public async Task WriteChunksAsync(string filePath, IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var content = string.Join("\n\n", chunks.Select(c =>
            $"--- Page {c.PageNumber}, Chunk {c.ChunkIndex} ---\n{c.Content}"));
        await WriteTextFileAsync(filePath, content, cancellationToken);
    }
}
