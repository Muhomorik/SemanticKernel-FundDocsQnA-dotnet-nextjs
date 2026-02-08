namespace Preprocessor.Services;

/// <summary>
/// Service for splitting text into smaller chunks suitable for embedding generation and vector search.
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Splits text into smaller chunks suitable for embedding generation and vector search.
    /// </summary>
    /// <param name="text">The text to split into chunks.</param>
    /// <returns>Collection of text chunks.</returns>
    IEnumerable<string> Chunk(string text);
}
