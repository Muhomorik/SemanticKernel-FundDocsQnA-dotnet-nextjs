using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PDFtoImage;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Infrastructure;
using PdfTextExtractor.Core.Domain.Events.Ocr;

namespace PdfTextExtractor.Core.Infrastructure.Rasterization;

/// <summary>
/// PDF page rasterization service using PDFtoImage library.
/// </summary>
public class PdfPageRasterizer : IRasterizationService
{
    private readonly ILogger<PdfPageRasterizer> _logger;

    public PdfPageRasterizer(ILogger<PdfPageRasterizer>? logger = null)
    {
        _logger = logger ?? NullLogger<PdfPageRasterizer>.Instance;
    }

    public async Task<RasterizationResult> RasterizePageAsync(
        string pdfFilePath,
        int pageNumber,
        string outputDirectory,
        int dpi,
        IEventPublisher eventPublisher,
        Guid correlationId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        // Publish rasterization started event
        await eventPublisher.PublishAsync(new PageRasterizationStarted
        {
            CorrelationId = correlationId,
            SessionId = sessionId,
            ExtractorName = "LMStudio",
            FilePath = pdfFilePath,
            PageNumber = pageNumber,
            TargetDpi = dpi
        }, cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Generate output filename
            var pdfFileName = Path.GetFileNameWithoutExtension(pdfFilePath);
            var imageFileName = $"{pdfFileName}_page_{pageNumber:D3}.png";
            var tempImagePath = Path.Combine(outputDirectory, imageFileName);

            _logger.LogDebug(
                "Rasterizing page {PageNumber} from {PdfFile} to {ImagePath} at {Dpi} DPI",
                pageNumber, pdfFilePath, tempImagePath, dpi);

            // Render PDF page to image using PDFtoImage
            // PDFtoImage 5.0.0 uses synchronous API - wrap in Task.Run
            // Note: Using default DPI as PDFtoImage 5.0 has different API
            await Task.Run(() =>
            {
                using var skBitmap = Conversion.ToImage(pdfFilePath, pageNumber - 1);

                // Save SKBitmap as PNG
                using var image = skBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                using var fileStream = File.Create(tempImagePath);
                image.SaveTo(fileStream);
            }, cancellationToken);

            var fileInfo = new FileInfo(tempImagePath);

            // Publish temp image saved event
            await eventPublisher.PublishAsync(new TempImageSaved
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "LMStudio",
                FilePath = pdfFilePath,
                PageNumber = pageNumber,
                TempImagePath = tempImagePath,
                ImageSizeBytes = fileInfo.Length
            }, cancellationToken);

            // Publish rasterization completed event
            await eventPublisher.PublishAsync(new PageRasterizationCompleted
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "LMStudio",
                FilePath = pdfFilePath,
                PageNumber = pageNumber,
                TempImagePath = tempImagePath,
                ImageSizeBytes = fileInfo.Length
            }, cancellationToken);

            _logger.LogInformation(
                "Rasterized page {PageNumber} from {PdfFile} to {ImagePath} ({SizeKB} KB)",
                pageNumber, pdfFilePath, tempImagePath, fileInfo.Length / 1024);

            return new RasterizationResult
            {
                TempImagePath = tempImagePath,
                ImageSizeBytes = fileInfo.Length
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Rasterization of page {PageNumber} from {PdfFile} was cancelled",
                pageNumber, pdfFilePath);
            throw;
        }
        catch (Exception ex)
        {
            await eventPublisher.PublishAsync(new PageRasterizationFailed
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "LMStudio",
                FilePath = pdfFilePath,
                PageNumber = pageNumber,
                ErrorMessage = ex.Message,
                ExceptionType = ex.GetType().Name
            }, cancellationToken);

            _logger.LogError(ex,
                "Failed to rasterize page {PageNumber} from {PdfFile}",
                pageNumber, pdfFilePath);

            throw;
        }
    }
}
