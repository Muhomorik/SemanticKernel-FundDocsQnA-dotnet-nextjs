using Microsoft.Extensions.Logging;
using Preprocessor.Models;
using Preprocessor.Services;

namespace Preprocessor.Extractors;

/// <summary>
/// Extracts text from pre-generated text files (output from PdfTextExtractor).
/// Reads files matching pattern: {PdfBaseName}_page_{PageNumber}.txt
/// </summary>
/// <remarks>
/// IMPORTANT: Preserves all original text formatting from PdfTextExtractor output files.
/// Does NOT apply any text normalization or whitespace cleaning - formatting preservation is a requirement.
/// This includes paragraph breaks, line breaks, indentation, tables, and all other structural elements.
/// </remarks>
public class TextFileExtractor : IPdfExtractor
{
    private readonly ILogger<TextFileExtractor> _logger;
    private readonly ITextChunker _textChunker;

    public string MethodName => "textfile";

    public TextFileExtractor(ILogger<TextFileExtractor> logger, ITextChunker textChunker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textChunker = textChunker ?? throw new ArgumentNullException(nameof(textChunker));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads pre-extracted text files matching pattern {PdfBaseName}_page_{PageNumber}.txt.
    /// Text content is read directly from files without any cleaning or normalization.
    /// All formatting (line breaks, paragraph structure, tables, indentation) is preserved as-is per interface contract.
    /// </remarks>
    public async Task<IEnumerable<DocumentChunk>> ExtractAsync(string pdfFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pdfFilePath))
        {
            throw new ArgumentException("PDF file path cannot be null or empty.", nameof(pdfFilePath));
        }

        // 1. Get PDF filename and base name
        var pdfFileName = Path.GetFileName(pdfFilePath);
        var pdfBaseName = Path.GetFileNameWithoutExtension(pdfFilePath);
        var directory = Path.GetDirectoryName(pdfFilePath) ?? ".";

        _logger.LogDebug("Looking for text files for {PdfFile} in {Directory}", pdfFileName, directory);

        // 2. Find text files matching {basename}_page_*.txt
        var textFilePattern = $"{pdfBaseName}_page_*.txt";
        var textFiles = Directory.GetFiles(directory, textFilePattern, SearchOption.TopDirectoryOnly);

        if (textFiles.Length == 0)
        {
            _logger.LogWarning("No text files found for {PdfFile}, skipping", pdfFileName);
            return Enumerable.Empty<DocumentChunk>();
        }

        _logger.LogInformation("Found {Count} text files for {PdfFile}", textFiles.Length, pdfFileName);

        // 3. Parse page numbers and validate sequential ordering
        var pageFiles = textFiles
            .Select(f => new { FilePath = f, PageNumber = ExtractPageNumber(f, pdfBaseName) })
            .Where(x => x.PageNumber.HasValue)
            .OrderBy(x => x.PageNumber)
            .ToList();

        if (pageFiles.Count == 0)
        {
            _logger.LogWarning("No valid page files found for {PdfFile} (could not parse page numbers)", pdfFileName);
            return Enumerable.Empty<DocumentChunk>();
        }

        if (!ValidateSequentialPages(pageFiles.Select(p => p.PageNumber!.Value)))
        {
            var pageNumbers = string.Join(", ", pageFiles.Select(p => p.PageNumber));
            _logger.LogWarning("Non-sequential page numbers found for {PdfFile} (found pages: {PageNumbers}), skipping", pdfFileName, pageNumbers);
            return Enumerable.Empty<DocumentChunk>();
        }

        // 4. Read each page file and create chunks
        var chunks = new List<DocumentChunk>();

        foreach (var pageFile in pageFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageText = await File.ReadAllTextAsync(pageFile.FilePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(pageText))
            {
                _logger.LogDebug("Page {PageNumber} of {PdfFile} is empty, skipping", pageFile.PageNumber, pdfFileName);
                continue;
            }

            // 5. Chunk the text (preserve formatting from PdfTextExtractor)
            var pageChunks = _textChunker.Chunk(pageText).ToList();

            _logger.LogDebug("Created {ChunkCount} chunks from page {PageNumber} of {PdfFile}",
                pageChunks.Count, pageFile.PageNumber, pdfFileName);

            // 6. Create DocumentChunk objects with PDF filename (not text filename)
            for (var i = 0; i < pageChunks.Count; i++)
            {
                chunks.Add(new DocumentChunk
                {
                    SourceFile = pdfFileName,  // Use original PDF name
                    PageNumber = pageFile.PageNumber!.Value,
                    ChunkIndex = i,
                    Content = pageChunks[i]
                });
            }
        }

        _logger.LogInformation("Extracted {ChunkCount} total chunks from {PageCount} pages of {PdfFile}",
            chunks.Count, pageFiles.Count, pdfFileName);

        return chunks;
    }

    /// <summary>
    /// Extracts page number from text filename.
    /// Pattern: {basename}_page_{number}.txt
    /// </summary>
    private static int? ExtractPageNumber(string textFilePath, string pdfBaseName)
    {
        var fileName = Path.GetFileNameWithoutExtension(textFilePath);
        var prefix = $"{pdfBaseName}_page_";

        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var pageNumberStr = fileName.Substring(prefix.Length);
        return int.TryParse(pageNumberStr, out var pageNumber) ? pageNumber : null;
    }

    /// <summary>
    /// Validates that page numbers are sequential starting from 1 with no gaps.
    /// </summary>
    private static bool ValidateSequentialPages(IEnumerable<int> pageNumbers)
    {
        var pages = pageNumbers.OrderBy(p => p).ToList();

        if (pages.Count == 0)
        {
            return false;
        }

        // Must start at 1
        if (pages[0] != 1)
        {
            return false;
        }

        // Check sequential: each page = previous + 1
        for (int i = 1; i < pages.Count; i++)
        {
            if (pages[i] != pages[i - 1] + 1)
            {
                return false;
            }
        }

        return true;
    }

}
