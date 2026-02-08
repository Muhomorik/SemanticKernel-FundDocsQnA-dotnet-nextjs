using PdfTextExtractor.Core.Domain.Events;

namespace PdfTextExtractor.Core.Infrastructure.Rasterization;

/// <summary>
/// Service for rasterizing PDF pages to images for OCR processing.
/// </summary>
public interface IRasterizationService
{
    /// <summary>
    /// Rasterizes a single PDF page to a PNG image.
    /// </summary>
    /// <param name="pdfFilePath">Path to the PDF file.</param>
    /// <param name="pageNumber">1-based page number to rasterize.</param>
    /// <param name="outputDirectory">Directory where temp image will be saved.</param>
    /// <param name="dpi">Target DPI for rasterization (default: 300).</param>
    /// <param name="eventPublisher">Event publisher for lifecycle events.</param>
    /// <param name="correlationId">Correlation ID for event tracking.</param>
    /// <param name="sessionId">Session ID for event tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>RasterizationResult containing temp image path and metadata.</returns>
    Task<RasterizationResult> RasterizePageAsync(
        string pdfFilePath,
        int pageNumber,
        string outputDirectory,
        int dpi,
        IEventPublisher eventPublisher,
        Guid correlationId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a page rasterization operation.
/// </summary>
public record RasterizationResult
{
    /// <summary>
    /// Path to the temporary image file created.
    /// </summary>
    public required string TempImagePath { get; init; }

    /// <summary>
    /// Size of the image file in bytes.
    /// </summary>
    public long ImageSizeBytes { get; init; }
}
