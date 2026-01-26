using Microsoft.Extensions.Logging;

using Preprocessor.Models;
using Preprocessor.Services;

using UglyToad.PdfPig;

namespace Preprocessor.Extractors;

/// <summary>
/// Extracts text from PDF files using PdfPig library.
/// </summary>
public class PdfPigExtractor : IPdfExtractor
{
    private readonly ILogger<PdfPigExtractor> _logger;
    private readonly ITextChunker _textChunker;

    public string MethodName => "pdfpig";

    public PdfPigExtractor(ILogger<PdfPigExtractor> logger, ITextChunker textChunker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textChunker = textChunker ?? throw new ArgumentNullException(nameof(textChunker));
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
            var pageChunks = _textChunker.Chunk(pageText).ToList();

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
}