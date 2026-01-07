using Preprocessor.Models;

namespace Preprocessor.Services;

/// <summary>
/// Service for sanitizing document chunks by removing unwanted text patterns.
/// </summary>
public interface IChunkSanitizer
{
    /// <summary>
    /// Sanitizes a collection of document chunks by removing known noise patterns.
    /// </summary>
    /// <param name="chunks">The chunks to sanitize.</param>
    /// <returns>Sanitized chunks with cleaned content.</returns>
    IEnumerable<DocumentChunk> Sanitize(IEnumerable<DocumentChunk> chunks);
}
