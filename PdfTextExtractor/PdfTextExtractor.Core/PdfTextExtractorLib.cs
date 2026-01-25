using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Batch;
using PdfTextExtractor.Core.Domain.Events.Page;
using PdfTextExtractor.Core.Infrastructure.EventBus;
using PdfTextExtractor.Core.Infrastructure.Extractors;
using PdfTextExtractor.Core.Infrastructure.FileSystem;
using PdfTextExtractor.Core.Infrastructure.LMStudio;
using PdfTextExtractor.Core.Infrastructure.OpenAI;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Models;
using UglyToad.PdfPig;

namespace PdfTextExtractor.Core;

/// <summary>
/// Main entry point for PdfTextExtractor library with Autofac DI.
/// </summary>
public class PdfTextExtractorLib : IPdfTextExtractorLib, IDisposable
{
    private readonly IContainer _container;
    private readonly ReactiveEventPublisher _eventPublisher;
    private readonly ILoggerFactory? _loggerFactory;

    public PdfTextExtractorLib(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;

        var builder = new ContainerBuilder();

        // Register ILoggerFactory if provided (enables logging)
        if (_loggerFactory != null)
        {
            builder.RegisterInstance(_loggerFactory).As<ILoggerFactory>();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));
        }
        else
        {
            // Fallback: Register NullLogger<T> for design-time/test scenarios
            builder.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>));
        }

        // Register module
        builder.RegisterModule<PdfTextExtractorModule>();

        _container = builder.Build();
        _eventPublisher = _container.Resolve<ReactiveEventPublisher>();
    }

    public IObservable<PdfExtractionEventBase> Events => _eventPublisher.Events;

    public string[] GetPdfFiles(string folderPath)
    {
        var fileSystem = _container.Resolve<IFileSystemService>();
        return fileSystem.GetPdfFiles(folderPath);
    }

    public string[] GetTextFiles(string folderPath)
    {
        var fileSystem = _container.Resolve<IFileSystemService>();
        return fileSystem.GetTextFiles(folderPath);
    }

    public async Task<ExtractionResult> ExtractWithPdfPigAsync(
        PdfPigParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(parameters.PdfFolderPath, parameters.OutputFolderPath);

        var logger = _container.Resolve<ILogger<PdfPigExtractor>>();
        var extractor = new PdfPigExtractor(logger);
        return await ExtractCoreAsync(
            extractor,
            parameters.PdfFolderPath,
            parameters.OutputFolderPath,
            TextExtractionMethod.PdfPig,
            parameters.SkipIfExists,
            cancellationToken);
    }

    public async Task<ExtractionResult> ExtractWithLMStudioAsync(
        LMStudioParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(parameters.PdfFolderPath, parameters.OutputFolderPath);

        // Resolve dependencies from container
        var rasterizationService = _container.Resolve<IRasterizationService>();
        var visionLogger = _container.Resolve<ILogger<LMStudioVisionClient>>();
        var extractorLogger = _container.Resolve<ILogger<LMStudioOcrExtractor>>();

        // Manually construct vision client with maxTokens and extraction prompt from parameters
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var visionClient = new LMStudioVisionClient(visionLogger, httpClient, parameters.MaxTokens, parameters.ExtractionPrompt);

        // Manually construct extractor with parameters
        var extractor = new LMStudioOcrExtractor(
            extractorLogger,
            rasterizationService,
            visionClient,
            parameters);

        return await ExtractCoreAsync(
            extractor,
            parameters.PdfFolderPath,
            parameters.OutputFolderPath,
            TextExtractionMethod.LMStudio,
            parameters.SkipIfExists,
            cancellationToken);
    }

    public async Task<ExtractionResult> ExtractWithOpenAIAsync(
        OpenAIParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(parameters.PdfFolderPath, parameters.OutputFolderPath);

        // Resolve dependencies from container
        var rasterizationService = _container.Resolve<IRasterizationService>();
        var visionLogger = _container.Resolve<ILogger<OpenAIVisionClient>>();
        var extractorLogger = _container.Resolve<ILogger<OpenAIOcrExtractor>>();

        // Manually construct vision client with extraction prompt from parameters
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var visionClient = new OpenAIVisionClient(httpClient, visionLogger, parameters.ExtractionPrompt);

        // Manually construct extractor with parameters
        var extractor = new OpenAIOcrExtractor(
            extractorLogger,
            rasterizationService,
            visionClient,
            parameters);

        return await ExtractCoreAsync(
            extractor,
            parameters.PdfFolderPath,
            parameters.OutputFolderPath,
            TextExtractionMethod.OpenAI,
            parameters.SkipIfExists,
            cancellationToken);
    }

    private async Task<ExtractionResult> ExtractCoreAsync(
        IPdfTextExtractor extractor,
        string pdfFolderPath,
        string outputFolderPath,
        TextExtractionMethod method,
        bool skipIfExists,
        CancellationToken cancellationToken)
    {
        var fileSystem = _container.Resolve<IFileSystemService>();
        var textWriter = _container.Resolve<ITextFileWriter>();
        var logger = _container.Resolve<ILogger<PdfTextExtractorLib>>();

        fileSystem.EnsureDirectoryExists(outputFolderPath);

        var sessionId = Guid.NewGuid();
        var pdfFiles = fileSystem.GetPdfFiles(pdfFolderPath);
        var startTime = DateTimeOffset.UtcNow;

        // Publish batch started event
        await _eventPublisher.PublishAsync(new BatchExtractionStarted
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = sessionId,
            ExtractorName = method.ToString(),
            FilePaths = pdfFiles,
            TotalFiles = pdfFiles.Length
        }, cancellationToken);

        var allResults = new List<ExtractionResult>();

        foreach (var pdfFile in pdfFiles)
        {
            var correlationId = Guid.NewGuid();
            var documentStartTime = DateTimeOffset.UtcNow;

            try
            {
                var totalPages = GetPdfPageCount(pdfFile);
                var allPages = new List<DocumentPage>();
                var skippedPageNumbers = new HashSet<int>();

                // Phase 1: Pre-flight check - does merged file exist?
                if (skipIfExists)
                {
                    var mergedPath = BuildMergedTextFilePath(outputFolderPath, pdfFile);

                    if (fileSystem.FileExists(mergedPath))
                    {
                        try
                        {
                            var existingText = await fileSystem.ReadTextFileAsync(mergedPath, cancellationToken);

                            // Validate file is not empty
                            if (!string.IsNullOrWhiteSpace(existingText))
                            {
                                // Merged file exists and is valid - skip entire document
                                logger.LogInformation(
                                    "Skipped all {TotalPages} pages for {PdfFile}, merged file exists at {MergedFile}",
                                    totalPages, Path.GetFileName(pdfFile), Path.GetFileName(mergedPath));

                                // Mark all pages as skipped
                                for (int pageNumber = 1; pageNumber <= totalPages; pageNumber++)
                                {
                                    skippedPageNumbers.Add(pageNumber);
                                }

                                // Add result for skipped document
                                allResults.Add(new ExtractionResult
                                {
                                    PdfFilePath = pdfFile,
                                    PageTextFiles = mergedPath,
                                    TotalPages = totalPages,
                                    SkippedPages = totalPages,
                                    ExtractedPages = 0,
                                    Duration = DateTimeOffset.UtcNow - documentStartTime,
                                    Method = method,
                                    TotalPromptTokens = 0,
                                    TotalCompletionTokens = 0,
                                    TotalTokens = 0
                                });

                                // Continue to next PDF file
                                continue;
                            }
                            else
                            {
                                logger.LogWarning(
                                    "Existing merged file is empty or whitespace-only, will re-extract: {FilePath}",
                                    mergedPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex,
                                "Failed to read existing merged file {FilePath}, will re-extract",
                                mergedPath);
                        }
                    }
                }

                // Phase 2: Extract missing pages (if any)
                var missingPageCount = totalPages - skippedPageNumbers.Count;

                if (missingPageCount > 0)
                {
                    logger.LogInformation(
                        "Extracting {MissingPages} pages (skipped {SkippedPages}) from {PdfFile}",
                        missingPageCount, skippedPageNumbers.Count, Path.GetFileName(pdfFile));

                    // Call extractor for the entire document
                    // Note: Current extractor interface processes all pages; we'll extract all and filter
                    var extractedPages = await extractor.ExtractAsync(
                        pdfFile,
                        _eventPublisher,
                        correlationId,
                        sessionId,
                        cancellationToken);

                    // Filter to only pages that weren't skipped
                    foreach (var page in extractedPages)
                    {
                        if (!skippedPageNumbers.Contains(page.PageNumber))
                        {
                            // Note: WasSkipped defaults to false, so we can add the page directly
                            allPages.Add(page);
                        }
                    }

                    // Write merged document with all extracted pages
                    var pagesToWrite = allPages.Where(p => !p.WasSkipped).ToList();
                    if (pagesToWrite.Any())
                    {
                        await textWriter.WriteMergedDocumentAsync(outputFolderPath, pdfFile, allPages, cancellationToken);

                        // Delete legacy individual page files if they exist
                        DeleteLegacyPageFiles(outputFolderPath, pdfFile, totalPages, fileSystem, logger);
                    }
                }
                else
                {
                    logger.LogInformation(
                        "Skipped all {TotalPages} pages for {PdfFile}, all text files exist",
                        totalPages, Path.GetFileName(pdfFile));
                }

                // Phase 3: Build result (combine skipped + extracted pages)
                var sortedPages = allPages.OrderBy(p => p.PageNumber).ToList();
                var mergedFilePath = BuildMergedTextFilePath(outputFolderPath, pdfFile);

                var extractedCount = sortedPages.Count(p => !p.WasSkipped);
                var skippedCount = sortedPages.Count(p => p.WasSkipped);

                allResults.Add(new ExtractionResult
                {
                    PdfFilePath = pdfFile,
                    PageTextFiles = mergedFilePath,
                    TotalPages = sortedPages.Count,
                    SkippedPages = skippedCount,
                    ExtractedPages = extractedCount,
                    Duration = DateTimeOffset.UtcNow - documentStartTime,
                    Method = method,
                    TotalPromptTokens = sortedPages.Sum(p => p.PromptTokens),
                    TotalCompletionTokens = sortedPages.Sum(p => p.CompletionTokens),
                    TotalTokens = sortedPages.Sum(p => p.TotalTokens)
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Error already published by extractor
                throw;
            }
        }

        // Publish batch completed event
        await _eventPublisher.PublishAsync(new BatchExtractionCompleted
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = sessionId,
            ExtractorName = method.ToString(),
            OutputFilePaths = allResults.Select(r => r.PageTextFiles).ToArray(),
            TotalFilesProcessed = allResults.Count,
            TotalDuration = DateTimeOffset.UtcNow - startTime
        }, cancellationToken);

        // Return first result (or aggregate if needed)
        return allResults.FirstOrDefault() ?? throw new InvalidOperationException("No files processed.");
    }

    private int GetPdfPageCount(string pdfFilePath)
    {
        using var doc = PdfDocument.Open(pdfFilePath);
        return doc.NumberOfPages;
    }

    private string BuildMergedTextFilePath(string outputFolderPath, string pdfFilePath)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(pdfFilePath)}.txt";
        return Path.Combine(outputFolderPath, fileName);
    }

    private void DeleteLegacyPageFiles(string outputFolderPath, string pdfFile, int totalPages, IFileSystemService fileSystem, ILogger<PdfTextExtractorLib> logger)
    {
        var pdfFileName = Path.GetFileNameWithoutExtension(pdfFile);

        for (int pageNum = 1; pageNum <= totalPages; pageNum++)
        {
            var legacyPath = Path.Combine(outputFolderPath,
                $"{pdfFileName}_page_{pageNum}.txt");

            if (fileSystem.FileExists(legacyPath))
            {
                try
                {
                    File.Delete(legacyPath);
                    logger.LogDebug("Deleted legacy page file: {Path}", legacyPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete legacy page file: {Path}", legacyPath);
                    // Don't throw - cleanup is best-effort
                }
            }
        }
    }

    private void ValidateParameters(string pdfFolderPath, string outputFolderPath)
    {
        if (string.IsNullOrWhiteSpace(pdfFolderPath))
            throw new ArgumentException("PDF folder path cannot be empty.", nameof(pdfFolderPath));

        if (string.IsNullOrWhiteSpace(outputFolderPath))
            throw new ArgumentException("Output folder path cannot be empty.", nameof(outputFolderPath));

        if (!Directory.Exists(pdfFolderPath))
            throw new DirectoryNotFoundException($"PDF folder not found: {pdfFolderPath}");
    }

    public void Dispose()
    {
        _eventPublisher?.Dispose();
        _container?.Dispose();
    }
}

/// <summary>
/// Autofac module for registering all PdfTextExtractor services.
/// </summary>
public class PdfTextExtractorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Event publisher (singleton)
        builder.RegisterType<ReactiveEventPublisher>()
            .As<IEventPublisher>()
            .AsSelf()
            .SingleInstance();

        // File system services
        builder.RegisterType<FileSystemService>()
            .As<IFileSystemService>()
            .InstancePerDependency();

        builder.RegisterType<TextFileWriter>()
            .As<ITextFileWriter>()
            .InstancePerDependency();

        // Rasterization service
        builder.RegisterType<PdfPageRasterizer>()
            .As<IRasterizationService>()
            .InstancePerDependency();

        // LM Studio vision client
        builder.RegisterType<LMStudioVisionClient>()
            .As<ILMStudioVisionClient>()
            .InstancePerDependency()
            .WithParameter(new TypedParameter(typeof(HttpClient), new HttpClient { Timeout = TimeSpan.FromMinutes(5) }));

        // Extractors
        builder.RegisterType<PdfPigExtractor>()
            .As<IPdfTextExtractor>()
            .InstancePerDependency();

        builder.RegisterType<LMStudioOcrExtractor>()
            .As<IPdfTextExtractor>()
            .AsSelf()
            .InstancePerDependency();
    }
}