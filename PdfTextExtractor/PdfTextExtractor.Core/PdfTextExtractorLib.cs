using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Batch;
using PdfTextExtractor.Core.Infrastructure.EventBus;
using PdfTextExtractor.Core.Infrastructure.Extractors;
using PdfTextExtractor.Core.Infrastructure.FileSystem;
using PdfTextExtractor.Core.Infrastructure.LMStudio;
using PdfTextExtractor.Core.Infrastructure.OpenAI;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core;

/// <summary>
/// Main entry point for PdfTextExtractor library with Autofac DI.
/// </summary>
public class PdfTextExtractorLib : IPdfTextExtractorLib, IDisposable
{
    private readonly IContainer _container;
    private readonly ReactiveEventPublisher _eventPublisher;

    public PdfTextExtractorLib()
    {
        var builder = new ContainerBuilder();

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

        var logger = _container.ResolveOptional<ILogger<PdfPigExtractor>>()
            ?? NullLogger<PdfPigExtractor>.Instance;
        var extractor = new PdfPigExtractor(logger, parameters.ChunkSize);
        return await ExtractCoreAsync(
            extractor,
            parameters.PdfFolderPath,
            parameters.OutputFolderPath,
            TextExtractionMethod.PdfPig,
            cancellationToken);
    }

    public async Task<ExtractionResult> ExtractWithLMStudioAsync(
        LMStudioParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(parameters.PdfFolderPath, parameters.OutputFolderPath);

        // Resolve dependencies from container
        var rasterizationService = _container.Resolve<IRasterizationService>();
        var visionLogger = _container.ResolveOptional<ILogger<LMStudioVisionClient>>()
            ?? NullLogger<LMStudioVisionClient>.Instance;
        var extractorLogger = _container.ResolveOptional<ILogger<LMStudioOcrExtractor>>()
            ?? NullLogger<LMStudioOcrExtractor>.Instance;

        // Manually construct vision client with maxTokens from parameters
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var visionClient = new LMStudioVisionClient(visionLogger, httpClient, parameters.MaxTokens);

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
            cancellationToken);
    }

    public async Task<ExtractionResult> ExtractWithOllamaAsync(
        OllamaParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(parameters.PdfFolderPath, parameters.OutputFolderPath);

        var extractor = _container.Resolve<OllamaOcrExtractor>();
        return await ExtractCoreAsync(
            extractor,
            parameters.PdfFolderPath,
            parameters.OutputFolderPath,
            TextExtractionMethod.Ollama,
            cancellationToken);
    }

    public async Task<ExtractionResult> ExtractWithOpenAIAsync(
        OpenAIParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(parameters.PdfFolderPath, parameters.OutputFolderPath);

        // Resolve dependencies from container
        var rasterizationService = _container.Resolve<IRasterizationService>();
        var visionLogger = _container.ResolveOptional<ILogger<OpenAIVisionClient>>()
            ?? NullLogger<OpenAIVisionClient>.Instance;
        var extractorLogger = _container.ResolveOptional<ILogger<OpenAIOcrExtractor>>()
            ?? NullLogger<OpenAIOcrExtractor>.Instance;

        // Manually construct vision client
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var visionClient = new OpenAIVisionClient(httpClient, visionLogger);

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
            cancellationToken);
    }

    private async Task<ExtractionResult> ExtractCoreAsync(
        IPdfTextExtractor extractor,
        string pdfFolderPath,
        string outputFolderPath,
        TextExtractionMethod method,
        CancellationToken cancellationToken)
    {
        var fileSystem = _container.Resolve<IFileSystemService>();
        var textWriter = _container.Resolve<ITextFileWriter>();

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

            try
            {
                var chunks = await extractor.ExtractAsync(
                    pdfFile,
                    _eventPublisher,
                    correlationId,
                    sessionId,
                    cancellationToken);

                // Write to text file
                var outputFileName = Path.GetFileNameWithoutExtension(pdfFile) + ".txt";
                var outputPath = Path.Combine(outputFolderPath, outputFileName);
                await textWriter.WriteChunksAsync(outputPath, chunks, cancellationToken);

                allResults.Add(new ExtractionResult
                {
                    PdfFilePath = pdfFile,
                    TextFilePath = outputPath,
                    TotalPages = chunks.Select(c => c.PageNumber).Distinct().Count(),
                    TotalChunks = chunks.Count(),
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Method = method
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
            OutputFilePaths = allResults.Select(r => r.TextFilePath).ToArray(),
            TotalFilesProcessed = allResults.Count,
            TotalDuration = DateTimeOffset.UtcNow - startTime
        }, cancellationToken);

        // Return first result (or aggregate if needed)
        return allResults.FirstOrDefault() ?? throw new InvalidOperationException("No files processed.");
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
            .InstancePerDependency()
            .WithParameter(new TypedParameter(typeof(ILogger<PdfPageRasterizer>), NullLogger<PdfPageRasterizer>.Instance));

        // LM Studio vision client
        builder.RegisterType<LMStudioVisionClient>()
            .As<ILMStudioVisionClient>()
            .InstancePerDependency()
            .WithParameter(new TypedParameter(typeof(ILogger<LMStudioVisionClient>), NullLogger<LMStudioVisionClient>.Instance))
            .WithParameter(new TypedParameter(typeof(HttpClient), new HttpClient { Timeout = TimeSpan.FromMinutes(5) }));

        // Extractors
        builder.RegisterType<PdfPigExtractor>()
            .As<IPdfTextExtractor>()
            .InstancePerDependency();

        builder.RegisterType<LMStudioOcrExtractor>()
            .As<IPdfTextExtractor>()
            .AsSelf()
            .InstancePerDependency();

        builder.RegisterType<OllamaOcrExtractor>()
            .As<IPdfTextExtractor>()
            .AsSelf()
            .InstancePerDependency();
    }
}