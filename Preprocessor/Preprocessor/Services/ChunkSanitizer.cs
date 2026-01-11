using Preprocessor.Models;

namespace Preprocessor.Services;

/// <summary>
/// Default implementation of chunk sanitization.
/// Removes known noise patterns from extracted PDF text.
/// </summary>
public class ChunkSanitizer : IChunkSanitizer
{
    private static readonly string[] NoisePatterns =
    [
        "1 2 3 4 5 6 7"
    ];

    /// <inheritdoc />
    public IEnumerable<DocumentChunk> Sanitize(IEnumerable<DocumentChunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            var sanitizedContent = chunk.Content;

            foreach (var pattern in NoisePatterns)
            {
                sanitizedContent = sanitizedContent.Replace(pattern, string.Empty, StringComparison.Ordinal);
            }

            // Trim whitespace after removal
            sanitizedContent = sanitizedContent.Trim();

            yield return new DocumentChunk
            {
                SourceFile = chunk.SourceFile,
                PageNumber = chunk.PageNumber,
                ChunkIndex = chunk.ChunkIndex,
                Content = sanitizedContent
            };
        }
    }
}
