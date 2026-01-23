using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.OpenAI;

/// <summary>
/// Client for OpenAI Vision API text extraction from images.
/// </summary>
public interface IOpenAIVisionClient
{
    /// <summary>
    /// Extract text from an image using OpenAI Vision API.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <param name="apiKey">OpenAI API key.</param>
    /// <param name="modelName">Vision model name (e.g., gpt-4o, gpt-4o-mini).</param>
    /// <param name="maxTokens">Maximum tokens for response.</param>
    /// <param name="detailLevel">Image detail level: "low", "high", or "auto" (default: "high").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Vision extraction result containing extracted text and token usage.</returns>
    Task<VisionExtractionResult> ExtractTextFromImageAsync(
        string imagePath,
        string apiKey,
        string modelName,
        int maxTokens,
        string detailLevel = "high",
        CancellationToken cancellationToken = default);
}
