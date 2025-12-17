using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Preprocessor.Models;

namespace Preprocessor.Extractors;

/// <summary>
/// Extracts text from PDF files using Ollama Vision models.
/// Converts PDF pages to images and uses vision AI to extract text.
/// </summary>
public class OllamaVisionExtractor : IPdfExtractor
{
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<OllamaVisionExtractor> _logger;
    private readonly string _visionModel;

    public string MethodName => "ollama-vision";

    public OllamaVisionExtractor(
        IChatCompletionService chatService,
        ILogger<OllamaVisionExtractor> logger,
        string visionModel = "llava")
    {
        _chatService = chatService;
        _logger = logger;
        _visionModel = visionModel;
    }

    public async Task<IEnumerable<DocumentChunk>> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"PDF file not found: {filePath}", filePath);
        }

        var fileName = Path.GetFileName(filePath);
        var chunks = new List<DocumentChunk>();

        _logger.LogInformation("Extracting text from {FileName} using Ollama Vision ({Model})", fileName, _visionModel);

        // For vision extraction, we need to convert PDF pages to images
        // This is a placeholder - full implementation would require a PDF-to-image library
        // like Docnet.Core or PdfiumViewer

        _logger.LogWarning("Ollama Vision extraction is not fully implemented. " +
            "PDF-to-image conversion requires additional libraries (e.g., Docnet.Core). " +
            "Falling back to a stub implementation.");

        // Stub: Return empty for now, indicating this method needs PDF rendering support
        await Task.CompletedTask;

        return chunks;
    }

    /// <summary>
    /// Extracts text from an image using the vision model.
    /// </summary>
    private async Task<string> ExtractTextFromImageAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();

        // Convert image to base64
        var base64Image = Convert.ToBase64String(imageData);

        // Create message with image
        var message = new ChatMessageContentItemCollection
        {
            new TextContent("Extract all text from this image. Return only the text content, preserving the structure as much as possible."),
            new ImageContent(imageData, "image/png")
        };

        chatHistory.AddUserMessage(message);

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }
}
