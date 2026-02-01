namespace Preprocessor.Services;

/// <summary>
/// Splits text into semantically coherent chunks with overlap, optimized for RAG systems.
/// </summary>
/// <remarks>
/// This implementation follows 2025 RAG best practices:
/// - Preserves semantic boundaries (sections/paragraphs) to maintain context
/// - Uses 10-20% overlap between chunks to prevent information loss at boundaries
/// - Targets optimal chunk size of 500-1000 characters (256-512 tokens)
///
/// For fund documents (PRIIP KIDs, factsheets), this approach:
/// - Keeps complete sections together (e.g., "Vilka är kostnaderna?" stays intact)
/// - Maintains context across chunks via overlap (e.g., last paragraph of chunk N 
///   appears at start of chunk N+1)
/// - Enables precise retrieval (e.g., "management fees" query retrieves cost section, 
///   not risk disclosures)
///
/// Overlap is critical: when someone asks "What are the management fees for SEB Asienfond?",
/// the overlapping context helps the LLM understand relationships between chunks (e.g., 
/// seeing both the cost breakdown AND the preceding explanation improves answer quality).
/// </remarks>
public class SemanticChunker : ITextChunker
{
    private readonly int _maxChunkSize;
    private readonly double _overlapPercentage;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticChunker"/> class.
    /// </summary>
    /// <param name="maxChunkSize">Maximum size of each chunk in characters. Must be greater than zero. Default: 800 characters.</param>
    /// <param name="overlapPercentage">Percentage of chunk to overlap with next chunk (0.0 to 0.5). Default: 0.15 (15%).</param>
    /// <exception cref="ArgumentException">Thrown when maxChunkSize is less than or equal to zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when overlapPercentage is not between 0.0 and 0.5.</exception>
    public SemanticChunker(int maxChunkSize = 800, double overlapPercentage = 0.15)
    {
        if (maxChunkSize <= 0)
        {
            throw new ArgumentException("Chunk size must be greater than zero.", nameof(maxChunkSize));
        }

        if (overlapPercentage is < 0.0 or > 0.5)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapPercentage), 
                "Overlap percentage must be between 0.0 and 0.5 (0-50%).");
        }

        _maxChunkSize = maxChunkSize;
        _overlapPercentage = overlapPercentage;
    }

    /// <inheritdoc/>
    public IEnumerable<string> Chunk(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        // Split on paragraph boundaries (double newlines)
        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (paragraphs.Count == 0)
        {
            yield break;
        }

        var currentChunk = new List<string>();
        var currentLength = 0;
        string? previousChunk = null;

        for (var i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            var paragraphLength = paragraph.Length + 2; // +2 for "\n\n" separator

            // If adding this paragraph exceeds max size and we have content, yield current chunk
            if (currentLength + paragraphLength > _maxChunkSize && currentChunk.Count > 0)
            {
                var chunkText = string.Join("\n\n", currentChunk);
                yield return chunkText;
                
                previousChunk = chunkText;
                
                // Start new chunk with overlap from previous chunk (only if overlap > 0)
                if (_overlapPercentage > 0)
                {
                    var overlapSize = (int)(_maxChunkSize * _overlapPercentage);
                    var (overlapChunk, overlapLength) = GetOverlapParagraphs(currentChunk, overlapSize);
                    
                    currentChunk = overlapChunk;
                    currentLength = overlapLength;
                }
                else
                {
                    currentChunk.Clear();
                    currentLength = 0;
                }
            }

            // If a single paragraph exceeds max size, split it or yield as-is
            if (paragraph.Length > _maxChunkSize)
            {
                // If we have accumulated content, yield it first
                if (currentChunk.Count > 0)
                {
                    yield return string.Join("\n\n", currentChunk);
                    currentChunk.Clear();
                    currentLength = 0;
                }

                // Yield large paragraph as its own chunk (can't split mid-paragraph without losing semantic meaning)
                yield return paragraph;
                previousChunk = paragraph;
                continue;
            }

            // Add paragraph to current chunk
            currentChunk.Add(paragraph);
            currentLength += paragraphLength;
        }

        // Yield remaining content
        if (currentChunk.Count > 0)
        {
            yield return string.Join("\n\n", currentChunk);
        }
    }

    /// <summary>
    /// Gets paragraphs from the end of the chunk for overlap, targeting the specified overlap size.
    /// </summary>
    /// <param name="paragraphs">List of paragraphs in the current chunk.</param>
    /// <param name="targetOverlapSize">Target size for overlap in characters.</param>
    /// <returns>Tuple of (overlap paragraphs, total overlap length).</returns>
    private static (List<string> OverlapParagraphs, int OverlapLength) GetOverlapParagraphs(
        List<string> paragraphs, 
        int targetOverlapSize)
    {
        var overlapParagraphs = new List<string>();
        var overlapLength = 0;

        // Take paragraphs from the end until we reach target overlap size
        for (var i = paragraphs.Count - 1; i >= 0; i--)
        {
            var paragraph = paragraphs[i];
            var paragraphLength = paragraph.Length + 2; // +2 for "\n\n"

            if (overlapLength + paragraphLength > targetOverlapSize && overlapParagraphs.Count > 0)
            {
                break;
            }

            overlapParagraphs.Insert(0, paragraph);
            overlapLength += paragraphLength;
        }

        return (overlapParagraphs, overlapLength);
    }
}
