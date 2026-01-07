using System.Diagnostics;

namespace Preprocessor.Models;

/// <summary>
/// Represents a chunk of text extracted from a PDF document.
/// </summary>
[DebuggerDisplay("{SourceFile} Page {PageNumber} Chunk {ChunkIndex}: {ContentPreview,nq}")]
public class DocumentChunk
{
    /// <summary>
    /// The source PDF file name.
    /// </summary>
    public required string SourceFile { get; init; }

    /// <summary>
    /// The page number (1-based) from which this chunk was extracted.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// The index of this chunk within the page (0-based).
    /// </summary>
    public int ChunkIndex { get; init; }

    /// <summary>
    /// The extracted text content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Truncated content preview for debugger display (first 50 characters).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string ContentPreview =>
        Content.Length > 50 ? $"{Content[..50]}..." : Content;
}
