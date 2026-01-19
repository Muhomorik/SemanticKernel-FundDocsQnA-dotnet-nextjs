using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Document;
using PdfTextExtractor.Core.Domain.Events.Infrastructure;
using PdfTextExtractor.Core.Domain.Events.Ocr;
using PdfTextExtractor.Core.Domain.Events.Page;
using PdfTextExtractor.Core.Domain.Events.TextProcessing;
using PdfTextExtractor.Core.Infrastructure.OpenAI;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Models;
using UglyToad.PdfPig;

namespace PdfTextExtractor.Core.Infrastructure.Extractors;

/// <summary>
/// OpenAI OCR-based text extractor using vision models.
/// </summary>
public class OpenAIOcrExtractor : IPdfTextExtractor
{
    private readonly ILogger<OpenAIOcrExtractor> _logger;
    private readonly IRasterizationService _rasterizationService;
    private readonly IOpenAIVisionClient _visionClient;
    private readonly OpenAIParameters _parameters;

    public TextExtractionMethod Method => TextExtractionMethod.OpenAI;

    public OpenAIOcrExtractor(
        ILogger<OpenAIOcrExtractor> logger,
        IRasterizationService rasterizationService,
        IOpenAIVisionClient visionClient,
        OpenAIParameters parameters)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rasterizationService = rasterizationService ?? throw new ArgumentNullException(nameof(rasterizationService));
        _visionClient = visionClient ?? throw new ArgumentNullException(nameof(visionClient));
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
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
        var tempImagePaths = new List<string>();

        // Create session temp directory in Windows temp folder
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "SemanticKernel-FundDocsQnA-dotnet-nextjs",
            "PdfTextExtractor",
            "temp",
            sessionId.ToString());
        Directory.CreateDirectory(tempDir);

        _logger.LogInformation(
            "Starting OpenAI OCR extraction for {FilePath} using model {ModelName}",
            filePath, _parameters.VisionModelName);

        try
        {
            // Publish document started event
            await eventPublisher.PublishAsync(new DocumentExtractionStarted
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "OpenAI",
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length
            }, cancellationToken);

            // Get total page count
            int totalPages;
            using (var document = PdfDocument.Open(filePath))
            {
                totalPages = document.NumberOfPages;
            }

            _logger.LogDebug("Processing {TotalPages} pages from {FilePath}", totalPages, filePath);

            // Process each page
            for (int pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Publish page started event
                await eventPublisher.PublishAsync(new PageExtractionStarted
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "OpenAI",
                    FilePath = filePath,
                    PageNumber = pageNumber,
                    TotalPages = totalPages
                }, cancellationToken);

                // Step 1: Rasterize page
                var rasterizationResult = await _rasterizationService.RasterizePageAsync(
                    filePath,
                    pageNumber,
                    tempDir,
                    _parameters.RasterizationDpi,
                    eventPublisher,
                    correlationId,
                    sessionId,
                    cancellationToken);

                tempImagePaths.Add(rasterizationResult.TempImagePath);

                // Step 2: OCR processing
                var ocrStartTime = DateTimeOffset.UtcNow;

                await eventPublisher.PublishAsync(new OcrProcessingStarted
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "OpenAI",
                    FilePath = filePath,
                    PageNumber = pageNumber,
                    VisionModelName = _parameters.VisionModelName
                }, cancellationToken);

                var extractedText = await _visionClient.ExtractTextFromImageAsync(
                    rasterizationResult.TempImagePath,
                    _parameters.ApiKey,
                    _parameters.VisionModelName,
                    _parameters.MaxTokens,
                    _parameters.DetailLevel,
                    cancellationToken);

                var cleanedText = CleanText(extractedText);

                await eventPublisher.PublishAsync(new OcrProcessingCompleted
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "OpenAI",
                    FilePath = filePath,
                    PageNumber = pageNumber,
                    ExtractedTextLength = cleanedText.Length,
                    ProcessingDuration = DateTimeOffset.UtcNow - ocrStartTime,
                    ExtractedText = cleanedText
                }, cancellationToken);

                // Check for empty page
                if (string.IsNullOrWhiteSpace(cleanedText))
                {
                    await eventPublisher.PublishAsync(new EmptyPageDetected
                    {
                        CorrelationId = correlationId,
                        SessionId = sessionId,
                        ExtractorName = "OpenAI",
                        FilePath = filePath,
                        PageNumber = pageNumber
                    }, cancellationToken);

                    _logger.LogWarning("Page {PageNumber} in {FilePath} is empty", pageNumber, filePath);
                    continue;
                }

                // Step 3: Chunk text
                var pageChunks = ChunkText(cleanedText, filePath, pageNumber);
                chunks.AddRange(pageChunks);

                // Publish text chunked event
                await eventPublisher.PublishAsync(new TextChunked
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "OpenAI",
                    FilePath = filePath,
                    PageNumber = pageNumber,
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
                        ExtractorName = "OpenAI",
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
                    ExtractorName = "OpenAI",
                    FilePath = filePath,
                    PageNumber = pageNumber,
                    ExtractedTextLength = cleanedText.Length,
                    ChunkCount = pageChunks.Count
                }, cancellationToken);

                // Publish progress event
                await eventPublisher.PublishAsync(new ExtractionProgressUpdated
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "OpenAI",
                    FilePath = filePath,
                    OverallPercentage = (double)pageNumber / totalPages * 100,
                    PagesProcessed = pageNumber,
                    TotalPages = totalPages,
                    CurrentOperation = $"Processing page {pageNumber} of {totalPages}"
                }, cancellationToken);
            }

            // Publish document completed event
            await eventPublisher.PublishAsync(new DocumentExtractionCompleted
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "OpenAI",
                FilePath = filePath,
                TotalPages = totalPages,
                TotalChunks = chunks.Count,
                OutputFilePath = "", // Set by caller
                Duration = DateTimeOffset.UtcNow - startTime
            }, cancellationToken);

            _logger.LogInformation(
                "Successfully extracted {ChunkCount} chunks from {TotalPages} pages in {FilePath}",
                chunks.Count, totalPages, filePath);

            return chunks;
        }
        catch (OperationCanceledException)
        {
            await eventPublisher.PublishAsync(new DocumentExtractionCancelled
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "OpenAI",
                FilePath = filePath,
                PagesProcessedBeforeCancellation = chunks.Select(c => c.PageNumber).Distinct().Count()
            }, cancellationToken);

            _logger.LogWarning("Extraction cancelled for {FilePath}", filePath);
            throw;
        }
        catch (Exception ex)
        {
            await eventPublisher.PublishAsync(new DocumentExtractionFailed
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "OpenAI",
                FilePath = filePath,
                ErrorMessage = ex.Message,
                ExceptionType = ex.GetType().Name,
                PageNumberWhereFailed = chunks.Select(c => c.PageNumber).Distinct().Count() + 1
            }, cancellationToken);

            _logger.LogError(ex, "Extraction failed for {FilePath}", filePath);
            throw;
        }
        finally
        {
            // Cleanup temp files
            await CleanupTempFilesAsync(tempImagePaths, tempDir, eventPublisher, correlationId, sessionId);
        }
    }

    private async Task CleanupTempFilesAsync(
        List<string> tempImagePaths,
        string tempDirectory,
        IEventPublisher eventPublisher,
        Guid correlationId,
        Guid sessionId)
    {
        var deletedFiles = new List<string>();

        _logger.LogDebug("Cleaning up {Count} temp files", tempImagePaths.Count);

        foreach (var tempPath in tempImagePaths)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                    deletedFiles.Add(tempPath);
                    _logger.LogTrace("Deleted temp file: {TempPath}", tempPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temp file: {TempPath}", tempPath);
            }
        }

        // Try to delete temp directory if empty
        try
        {
            if (Directory.Exists(tempDirectory) && !Directory.EnumerateFileSystemEntries(tempDirectory).Any())
            {
                Directory.Delete(tempDirectory);
                _logger.LogDebug("Deleted temp directory: {TempDirectory}", tempDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp directory: {TempDirectory}", tempDirectory);
        }

        if (deletedFiles.Any())
        {
            await eventPublisher.PublishAsync(new TempFilesCleanedUp
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "OpenAI",
                DeletedFilePaths = deletedFiles.ToArray(),
                TotalFilesDeleted = deletedFiles.Count
            });

            _logger.LogInformation("Cleaned up {Count} temp files", deletedFiles.Count);
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
            if (currentChunk.Length + sentence.Length > _parameters.ChunkSize && currentChunk.Length > 0)
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
