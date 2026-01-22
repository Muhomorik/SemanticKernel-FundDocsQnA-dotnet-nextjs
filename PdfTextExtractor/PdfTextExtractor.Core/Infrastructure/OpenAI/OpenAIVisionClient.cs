using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PdfTextExtractor.Core.Infrastructure.OpenAI;

/// <summary>
/// Client for OpenAI Vision API text extraction from images.
/// </summary>
public class OpenAIVisionClient : IOpenAIVisionClient
{
    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";
    private readonly ILogger<OpenAIVisionClient> _logger;
    private readonly HttpClient _httpClient;

    public OpenAIVisionClient(HttpClient httpClient, ILogger<OpenAIVisionClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ExtractTextFromImageAsync(
        string imagePath,
        string apiKey,
        string modelName,
        int maxTokens,
        string detailLevel = "high",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path cannot be empty.", nameof(imagePath));

        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}", imagePath);

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("OpenAI API key cannot be empty.", nameof(apiKey));

        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty.", nameof(modelName));

        if (string.IsNullOrWhiteSpace(detailLevel))
            throw new ArgumentException("Detail level cannot be empty.", nameof(detailLevel));

        try
        {
            _logger.LogDebug(
                "Extracting text from {ImagePath} using OpenAI model {ModelName} (detail: {DetailLevel})",
                imagePath, modelName, detailLevel);

            // Read image and convert to base64
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);
            var dataUri = $"data:image/png;base64,{base64Image}";

            // Construct request payload (OpenAI format)
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
                            new { type = "text", text = "Extract all visible text from this image. Return only the text content, preserving formatting and structure. Do not add explanations or commentary." },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = dataUri,
                                    detail = detailLevel
                                }
                            }
                        }
                    }
                },
                max_tokens = maxTokens
            };

            // Create HTTP request with Authorization header
            var requestJson = JsonSerializer.Serialize(requestPayload);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, OpenAIEndpoint)
            {
                Content = requestContent
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            _logger.LogDebug("Sending OCR request to OpenAI endpoint: {Endpoint}", OpenAIEndpoint);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            // Read response body (needed for both success and error cases)
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.LogError(
                    "OpenAI API returned HTTP {StatusCode}. Response: {ResponseBody}",
                    statusCode, responseBody);

                // Provide user-friendly error messages
                var errorMessage = statusCode switch
                {
                    401 => "Invalid OpenAI API key. Please check your API key and try again.",
                    429 => "OpenAI API rate limit exceeded. Please try again later or check your usage quota.",
                    500 or 502 or 503 => "OpenAI API server error. Please try again later.",
                    _ => $"OpenAI API returned HTTP {statusCode}. Response: {responseBody}"
                };

                throw new HttpRequestException(errorMessage);
            }

            // Parse response
            using var jsonDoc = JsonDocument.Parse(responseBody);

            var extractedText = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // Extract and log token usage
            if (jsonDoc.RootElement.TryGetProperty("usage", out var usageElement))
            {
                var promptTokens = usageElement.GetProperty("prompt_tokens").GetInt32();
                var completionTokens = usageElement.GetProperty("completion_tokens").GetInt32();
                var totalTokens = usageElement.GetProperty("total_tokens").GetInt32();

                _logger.LogInformation(
                    "OpenAI Vision API token usage - Image: {ImagePath}, Model: {ModelName}, " +
                    "Prompt tokens: {PromptTokens}, Completion tokens: {CompletionTokens}, Total tokens: {TotalTokens}",
                    imagePath, modelName, promptTokens, completionTokens, totalTokens);
            }
            else
            {
                _logger.LogWarning(
                    "Token usage information not found in OpenAI API response for {ImagePath}",
                    imagePath);
            }

            _logger.LogInformation(
                "Successfully extracted {TextLength} characters from {ImagePath} using OpenAI {ModelName}",
                extractedText?.Length ?? 0, imagePath, modelName);

            return extractedText ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP request to OpenAI API failed. Error: {Message}",
                ex.Message);
            throw new InvalidOperationException(
                $"Failed to connect to OpenAI API: {ex.Message}",
                ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI API response");
            throw new InvalidOperationException("OpenAI API returned an invalid JSON response.", ex);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OCR request to OpenAI was cancelled");
            throw;
        }
    }
}
