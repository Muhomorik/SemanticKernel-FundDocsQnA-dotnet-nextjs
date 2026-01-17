using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Document;
using PdfTextExtractor.Core.Domain.Events.Infrastructure;
using PdfTextExtractor.Core.Domain.Events.Page;
using PdfTextExtractor.Core.Domain.Events.TextProcessing;
using PdfTextExtractor.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfTextExtractor.Core.Infrastructure.Extractors;

/// <summary>
/// PdfPig-based text extractor for native PDF text extraction.
/// </summary>
public class PdfPigExtractor : IPdfTextExtractor
{
    private readonly ILogger<PdfPigExtractor> _logger;
    private readonly int _chunkSize;

    public TextExtractionMethod Method => TextExtractionMethod.PdfPig;

    public PdfPigExtractor(ILogger<PdfPigExtractor> logger, int chunkSize = 1000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chunkSize = chunkSize;
    }

    public async Task<IEnumerable<DocumentChunk>> ExtractAsync(
        string filePath,
        IEventPublisher eventPublisher,
        Guid correlationId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var chunks = new List<DocumentChunk>();

        // Publish document started event
        await eventPublisher.PublishAsync(new DocumentExtractionStarted
        {
            CorrelationId = correlationId,
            SessionId = sessionId,
            ExtractorName = "PdfPig",
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileSizeBytes = new FileInfo(filePath).Length
        }, cancellationToken);

        try
        {
            using var document = PdfDocument.Open(filePath);
            var totalPages = document.NumberOfPages;

            for (int i = 1; i <= totalPages; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var page = document.GetPage(i);

                // Publish page started event
                await eventPublisher.PublishAsync(new PageExtractionStarted
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "PdfPig",
                    FilePath = filePath,
                    PageNumber = i,
                    TotalPages = totalPages
                }, cancellationToken);

                // Extract text
                var words = page.GetWords();
                var pageText = string.Join(" ", words.Select(w => w.Text));
                var cleanedText = CleanText(pageText);

                // Check for empty page
                if (string.IsNullOrWhiteSpace(cleanedText))
                {
                    await eventPublisher.PublishAsync(new EmptyPageDetected
                    {
                        CorrelationId = correlationId,
                        SessionId = sessionId,
                        ExtractorName = "PdfPig",
                        FilePath = filePath,
                        PageNumber = i
                    }, cancellationToken);
                    continue;
                }

                // Chunk text
                var pageChunks = ChunkText(cleanedText, filePath, i);
                chunks.AddRange(pageChunks);

                // Publish text chunked event
                await eventPublisher.PublishAsync(new TextChunked
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "PdfPig",
                    FilePath = filePath,
                    PageNumber = i,
                    ChunkCount = pageChunks.Count,
                    ChunkSizes = pageChunks.Select(c => c.Content.Length).ToArray()
                }, cancellationToken);

                // Publish chunk created events
                foreach (var chunk in pageChunks)
                {
                    await eventPublisher.PublishAsync(new ChunkCreated
                    {
                        CorrelationId = correlationId,
                        SessionId = sessionId,
                        ExtractorName = "PdfPig",
                        FilePath = filePath,
                        PageNumber = chunk.PageNumber,
                        ChunkIndex = chunk.ChunkIndex,
                        ContentLength = chunk.Content.Length,
                        ContentPreview = chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length))
                    }, cancellationToken);
                }

                // Publish page completed event
                await eventPublisher.PublishAsync(new PageExtractionCompleted
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "PdfPig",
                    FilePath = filePath,
                    PageNumber = i,
                    ExtractedTextLength = cleanedText.Length,
                    ChunkCount = pageChunks.Count
                }, cancellationToken);

                // Publish progress event
                await eventPublisher.PublishAsync(new ExtractionProgressUpdated
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "PdfPig",
                    FilePath = filePath,
                    OverallPercentage = (double)i / totalPages * 100,
                    PagesProcessed = i,
                    TotalPages = totalPages,
                    CurrentOperation = $"Processing page {i} of {totalPages}"
                }, cancellationToken);
            }

            // Publish document completed event
            await eventPublisher.PublishAsync(new DocumentExtractionCompleted
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "PdfPig",
                FilePath = filePath,
                TotalPages = totalPages,
                TotalChunks = chunks.Count,
                OutputFilePath = "", // Set by caller
                Duration = DateTimeOffset.UtcNow - startTime
            }, cancellationToken);

            return chunks;
        }
        catch (OperationCanceledException)
        {
            await eventPublisher.PublishAsync(new DocumentExtractionCancelled
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "PdfPig",
                FilePath = filePath,
                PagesProcessedBeforeCancellation = chunks.Select(c => c.PageNumber).Distinct().Count()
            }, cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await eventPublisher.PublishAsync(new DocumentExtractionFailed
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "PdfPig",
                FilePath = filePath,
                ErrorMessage = ex.Message,
                ExceptionType = ex.GetType().Name,
                PageNumberWhereFailed = null
            }, cancellationToken);
            throw;
        }
    }

    private string CleanText(string text)
    {
        // Remove excessive whitespace
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private List<DocumentChunk> ChunkText(string text, string sourceFile, int pageNumber)
    {
        var chunks = new List<DocumentChunk>();
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
        var currentChunk = new StringBuilder();
        int chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > _chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    SourceFile = sourceFile,
                    PageNumber = pageNumber,
                    ChunkIndex = chunkIndex++,
                    Content = currentChunk.ToString().Trim()
                });
                currentChunk.Clear();
            }

            currentChunk.Append(sentence).Append(" ");
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                SourceFile = sourceFile,
                PageNumber = pageNumber,
                ChunkIndex = chunkIndex,
                Content = currentChunk.ToString().Trim()
            });
        }

        return chunks;
    }
}
