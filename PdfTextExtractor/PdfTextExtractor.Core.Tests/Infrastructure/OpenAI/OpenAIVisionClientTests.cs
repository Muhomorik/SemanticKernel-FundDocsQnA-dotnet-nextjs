using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using PdfTextExtractor.Core.Infrastructure.OpenAI;
using PdfTextExtractor.Core.Tests.AutoFixture;
using System.Net;
using System.Text.Json;

namespace PdfTextExtractor.Core.Tests.Infrastructure.OpenAI;

[TestFixture]
[TestOf(typeof(OpenAIVisionClient))]
public class OpenAIVisionClientTests
{
    private const string DefaultVisionModel = "gpt-4o";
    private const string DefaultApiKey = "test-api-key";
    private const string DefaultDetailLevel = "high";
    private const string DefaultExtractionPrompt = "Extract all visible text from this image. Return only the text content, preserving formatting and structure. Do not add explanations or commentary.";

    private IFixture _fixture;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private Mock<ILogger<OpenAIVisionClient>> _loggerMock;
    private HttpClient _httpClient;
    private OpenAIVisionClient _sut;
    private readonly List<string> _tempFilesToCleanup = new();

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<OpenAIVisionClient>>>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _sut = new OpenAIVisionClient(_httpClient, _loggerMock.Object, DefaultExtractionPrompt);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up all temp files created during the test
        foreach (var tempFile in _tempFilesToCleanup)
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }

        _tempFilesToCleanup.Clear();
    }

    /// <summary>
    /// Gets the correct temp directory path and creates a unique temp file path for the current test.
    /// Automatically registers the file for cleanup in TearDown.
    /// </summary>
    private string GetTestTempFilePath(string extension = ".png")
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "SemanticKernel-FundDocsQnA-dotnet-nextjs",
            "PdfTextExtractor",
            "temp");

        Directory.CreateDirectory(tempDir);

        // Use test name to create unique filename for this specific test
        var testName = TestContext.CurrentContext.Test.Name;
        var uniqueFileName = $"{testName}_{Guid.NewGuid()}{extension}";

        var filePath = Path.Combine(tempDir, uniqueFileName);

        // Register for cleanup
        _tempFilesToCleanup.Add(filePath);

        return filePath;
    }

    [Test]
    public async Task ExtractTextFromImageAsync_ValidResponse_ReturnsExtractedText()
    {
        // Arrange
        var imagePath = GetTestTempFilePath();
        await File.WriteAllBytesAsync(imagePath, new byte[] { 1, 2, 3 });

        var responseContent = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Extracted text from image"
                    }
                }
            },
            usage = new
            {
                prompt_tokens = 100,
                completion_tokens = 50,
                total_tokens = 150
            }
        });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _sut.ExtractTextFromImageAsync(
            imagePath,
            DefaultApiKey,
            DefaultVisionModel,
            2000,
            DefaultDetailLevel);

        // Assert
        Assert.That(result, Is.EqualTo("Extracted text from image"));
    }

    [Test]
    public async Task ExtractTextFromImageAsync_CustomPrompt_SendsCustomPromptInRequest()
    {
        // Arrange
        var customPrompt = "Custom extraction instruction for testing";
        var customClient = new OpenAIVisionClient(_httpClient, _loggerMock.Object, customPrompt);

        var imagePath = GetTestTempFilePath();
        await File.WriteAllBytesAsync(imagePath, new byte[] { 1, 2, 3 });

        string? capturedRequestBody = null;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedRequestBody = req.Content?.ReadAsStringAsync().Result;
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    choices = new[]
                    {
                        new
                        {
                            message = new
                            {
                                content = "Extracted text"
                            }
                        }
                    },
                    usage = new
                    {
                        prompt_tokens = 100,
                        completion_tokens = 50,
                        total_tokens = 150
                    }
                }))
            });

        // Act
        await customClient.ExtractTextFromImageAsync(
            imagePath,
            DefaultApiKey,
            DefaultVisionModel,
            2000,
            DefaultDetailLevel);

        // Assert
        Assert.That(capturedRequestBody, Is.Not.Null);
        Assert.That(capturedRequestBody, Does.Contain(customPrompt));
    }

    [Test]
    public async Task ExtractTextFromImageAsync_EmptyResponse_ReturnsEmptyString()
    {
        // Arrange
        var imagePath = GetTestTempFilePath();
        await File.WriteAllBytesAsync(imagePath, new byte[] { 1, 2, 3 });

        var responseContent = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = ""
                    }
                }
            },
            usage = new
            {
                prompt_tokens = 100,
                completion_tokens = 0,
                total_tokens = 100
            }
        });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _sut.ExtractTextFromImageAsync(
            imagePath,
            DefaultApiKey,
            DefaultVisionModel,
            2000,
            DefaultDetailLevel);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ExtractTextFromImageAsync_HttpError_ThrowsInvalidOperationException()
    {
        // Arrange
        var imagePath = GetTestTempFilePath();
        await File.WriteAllBytesAsync(imagePath, new byte[] { 1, 2, 3 });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server error")
            });

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExtractTextFromImageAsync(
            imagePath,
            DefaultApiKey,
            DefaultVisionModel,
            2000,
            DefaultDetailLevel));
    }

    [Test]
    public async Task ExtractTextFromImageAsync_InvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var imagePath = GetTestTempFilePath();
        await File.WriteAllBytesAsync(imagePath, new byte[] { 1, 2, 3 });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ invalid json }")
            });

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExtractTextFromImageAsync(
            imagePath,
            DefaultApiKey,
            DefaultVisionModel,
            2000,
            DefaultDetailLevel));
    }

    [Test]
    public void ExtractTextFromImageAsync_MissingImageFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(async () => await _sut.ExtractTextFromImageAsync(
            nonExistentPath,
            DefaultApiKey,
            DefaultVisionModel,
            2000,
            DefaultDetailLevel));
    }
}
