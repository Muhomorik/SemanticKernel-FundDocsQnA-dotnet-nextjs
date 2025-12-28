using Microsoft.Extensions.Logging;

using Preprocessor.Models;

using UglyToad.PdfPig;

namespace Preprocessor.Extractors;

/// <summary>
/// Extracts text from PDF files using PdfPig library.
/// </summary>
public class PdfPigExtractor : IPdfExtractor
{
    private readonly ILogger<PdfPigExtractor> _logger;
    private readonly int _chunkSize;

    public string MethodName => "pdfpig";

    public PdfPigExtractor(ILogger<PdfPigExtractor> logger, int chunkSize = 1000)
    {
        _logger = logger;
        _chunkSize = chunkSize;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<DocumentChunk>> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"PDF file not found: {filePath}", filePath);
        }

        var fileName = Path.GetFileName(filePath);
        var chunks = new List<DocumentChunk>();

        _logger.LogInformation("Extracting text from {FileName} using PdfPig", fileName);

        using var document = PdfDocument.Open(filePath);

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Extract words and join them with spaces for better spacing
            var words = page.GetWords();
            var pageText = string.Join(" ", words.Select(w => w.Text));

            if (string.IsNullOrWhiteSpace(pageText))
            {
                _logger.LogDebug("Page {PageNumber} of {FileName} has no text", page.Number, fileName);
                continue;
            }

            // Clean up the text
            pageText = CleanText(pageText);

            // Split into chunks
            var pageChunks = SplitIntoChunks(pageText, _chunkSize);

            for (var i = 0; i < pageChunks.Count; i++)
            {
                chunks.Add(new DocumentChunk
                {
                    SourceFile = fileName,
                    PageNumber = page.Number,
                    ChunkIndex = i,
                    Content = pageChunks[i]
                });
            }

            _logger.LogDebug("Extracted {ChunkCount} chunks from page {PageNumber} of {FileName}",
                pageChunks.Count, page.Number, fileName);
        }

        _logger.LogInformation("Extracted {TotalChunks} chunks from {FileName}", chunks.Count, fileName);

        return Task.FromResult<IEnumerable<DocumentChunk>>(chunks);
    }

    /// <summary>
    /// Cleans and normalizes extracted text by removing excessive whitespace.
    /// </summary>
    /// <param name="text">The raw text to clean.</param>
    /// <returns>Cleaned text with normalized whitespace.</returns>
    private static string CleanText(string text)
    {
        // Replace multiple whitespace with single space
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        // Trim
        text = text.Trim();

        return text;
    }

    /// <summary>
    /// Splits text into smaller chunks suitable for embedding generation and vector search.
    /// Attempts to split on sentence boundaries to maintain semantic coherence.
    /// </summary>
    /// <param name="text">The text to split into chunks.</param>
    /// <param name="chunkSize">Maximum size of each chunk in characters.</param>
    /// <returns>List of text chunks, each preserving sentence boundaries where possible.</returns>
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
    /// For fund documents Q&A, when someone asks "What are the management fees for SEB Asienfond?",
    /// the system can retrieve just the chunk containing the cost breakdown rather than irrelevant
    /// text about investment objectives or risk disclosures.
    /// </remarks>
    private static List<string> SplitIntoChunks(string text, int chunkSize)
    {
        var chunks = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            return chunks;
        }

        // Try to split on sentence boundaries
        var sentences = text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = string.Empty;

        foreach (var sentence in sentences)
        {
            var sentenceWithPeriod = sentence.TrimEnd('.', '!', '?') + ". ";

            if (currentChunk.Length + sentenceWithPeriod.Length > chunkSize && !string.IsNullOrEmpty(currentChunk))
            {
                chunks.Add(currentChunk.Trim());
                currentChunk = string.Empty;
            }

            currentChunk += sentenceWithPeriod;
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(currentChunk.Trim());
        }

        return chunks;
    }
}