using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PdfTextExtractor.Core.Infrastructure.LMStudio;

/// <summary>
/// Client for LM Studio vision API (OpenAI-compatible format).
/// </summary>
public class LMStudioVisionClient : ILMStudioVisionClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LMStudioVisionClient> _logger;

    public LMStudioVisionClient(HttpClient httpClient, ILogger<LMStudioVisionClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullLogger<LMStudioVisionClient>.Instance;
    }

    public async Task<string> ExtractTextFromImageAsync(
        string imagePath,
        string modelName,
        string lmStudioUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path cannot be empty.", nameof(imagePath));

        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}", imagePath);

        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty.", nameof(modelName));

        if (string.IsNullOrWhiteSpace(lmStudioUrl))
            throw new ArgumentException("LM Studio URL cannot be empty.", nameof(lmStudioUrl));

        try
        {
            _logger.LogDebug(
                "Extracting text from {ImagePath} using model {ModelName} at {LMStudioUrl}",
                imagePath, modelName, lmStudioUrl);

            // Read image and convert to base64
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);
            var dataUri = $"data:image/png;base64,{base64Image}";

            // Construct request payload (OpenAI-compatible format)
            var requestPayload = new
            {
                model = modelName,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Extract all text from this image. Return only the text, no explanations." },
                            new { type = "image_url", image_url = new { url = dataUri } }
                        }
                    }
                },
                temperature = 0.1,
                max_tokens = 4096
            };

            // Send HTTP request
            var endpoint = $"{lmStudioUrl.TrimEnd('/')}/v1/chat/completions";
            var requestJson = JsonSerializer.Serialize(requestPayload);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending OCR request to {Endpoint}", endpoint);

            var response = await _httpClient.PostAsync(endpoint, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Parse response
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = JsonDocument.Parse(responseBody);

            var extractedText = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogInformation(
                "Successfully extracted {TextLength} characters from {ImagePath}",
                extractedText?.Length ?? 0, imagePath);

            return extractedText ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP request to LM Studio failed. Ensure LM Studio is running at {LMStudioUrl} with model {ModelName} loaded.",
                lmStudioUrl, modelName);
            throw new InvalidOperationException(
                $"Failed to connect to LM Studio at {lmStudioUrl}. Ensure the server is running and the vision model '{modelName}' is loaded.",
                ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LM Studio API response");
            throw new InvalidOperationException("LM Studio returned an invalid JSON response.", ex);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OCR request to LM Studio was cancelled");
            throw;
        }
    }
}
