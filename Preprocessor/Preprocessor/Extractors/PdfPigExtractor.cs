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

            var pageText = page.Text;
            if (string.IsNullOrWhiteSpace(pageText))
            {
                _logger.LogDebug("Page {PageNumber} of {FileName} has no text", page.Number, fileName);
                continue;
            }

            // Clean up the text
            pageText = CleanText(pageText);

            // Split into chunks
            var pageChunks = SplitIntoChunks(pageText, _chunkSize);

            for (int i = 0; i < pageChunks.Count; i++)
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

    private static string CleanText(string text)
    {
        // Replace multiple whitespace with single space
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        // Trim
        text = text.Trim();

        return text;
    }

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
