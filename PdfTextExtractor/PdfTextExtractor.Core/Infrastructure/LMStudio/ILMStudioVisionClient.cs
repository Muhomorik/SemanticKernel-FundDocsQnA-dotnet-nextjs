using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.LMStudio;

/// <summary>
/// Client for LM Studio vision API.
/// </summary>
public interface ILMStudioVisionClient
{
    /// <summary>
    /// Sends an image to LM Studio vision model for OCR processing.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <param name="modelName">Vision model name (e.g., "llava-v1.5-7b").</param>
    /// <param name="lmStudioUrl">LM Studio base URL (default: http://localhost:1234).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Vision extraction result containing extracted text and token usage.</returns>
    Task<VisionExtractionResult> ExtractTextFromImageAsync(
        string imagePath,
        string modelName,
        string lmStudioUrl,
        CancellationToken cancellationToken = default);
}
