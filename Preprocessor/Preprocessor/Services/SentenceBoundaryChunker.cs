namespace Preprocessor.Services;

/// <summary>
/// Splits text into chunks based on sentence boundaries while respecting a maximum chunk size.
/// </summary>
/// <remarks>
/// Chunking is essential for RAG systems because:
/// - Embedding models have token/character limits
/// - Smaller chunks provide better retrieval granularity
/// - Vector search accuracy improves with focused, coherent text segments
///
/// The method combines multiple sentences into chunks (not one-sentence-per-chunk) because:
/// - Single sentences (20-100 chars) lack context for quality embeddings
/// - Embedding models work best with paragraph-level context (100-1000 chars)
/// - Related information across consecutive sentences stays together
/// - Fewer, richer chunks enable faster vector search
///
/// For fund documents Q&amp;A, when someone asks "What are the management fees for SEB Asienfond?",
/// the system can retrieve just the chunk containing the cost breakdown rather than irrelevant
/// text about investment objectives or risk disclosures.
/// </remarks>
public class SentenceBoundaryChunker : ITextChunker
{
    private readonly int _maxChunkSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="SentenceBoundaryChunker"/> class.
    /// </summary>
    /// <param name="maxChunkSize">Maximum size of each chunk in characters. Must be greater than zero.</param>
    /// <exception cref="ArgumentException">Thrown when maxChunkSize is less than or equal to zero.</exception>
    public SentenceBoundaryChunker(int maxChunkSize = 1000)
    {
        if (maxChunkSize <= 0)
        {
            throw new ArgumentException("Chunk size must be greater than zero.", nameof(maxChunkSize));
        }

        _maxChunkSize = maxChunkSize;
    }

    /// <inheritdoc/>
    public IEnumerable<string> Chunk(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        // Try to split on sentence boundaries
        var sentences = text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = string.Empty;

        foreach (var sentence in sentences)
        {
            var sentenceWithPeriod = sentence.TrimEnd('.', '!', '?') + ". ";

            if (currentChunk.Length + sentenceWithPeriod.Length > _maxChunkSize && !string.IsNullOrEmpty(currentChunk))
            {
                yield return currentChunk.Trim();
                currentChunk = string.Empty;
            }

            currentChunk += sentenceWithPeriod;
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            yield return currentChunk.Trim();
        }
    }
}
