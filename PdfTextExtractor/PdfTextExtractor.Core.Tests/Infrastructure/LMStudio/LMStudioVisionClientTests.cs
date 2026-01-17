using AutoFixture;
using AutoFixture.AutoMoq;

using Microsoft.Extensions.Logging;

using Moq;
using Moq.Protected;

using NUnit.Framework;

using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Infrastructure.LMStudio;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Tests.AutoFixture;
using PdfTextExtractor.Core.Tests.TestHelpers;

using System.Net;
using System.Text.Json;

namespace PdfTextExtractor.Core.Tests.Infrastructure.LMStudio;

[TestFixture]
[TestOf(typeof(LMStudioVisionClient))]
public class LMStudioVisionClientTests
{
    private const string DefaultVisionModel = "qwen/qwen2.5-vl-7b";
    private const string DefaultLMStudioUrl = "http://localhost:1234";

    private IFixture _fixture;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private Mock<ILogger<LMStudioVisionClient>> _loggerMock;
    private HttpClient _httpClient;
    private LMStudioVisionClient _sut;
    private readonly List<string> _tempFilesToCleanup = new();

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<LMStudioVisionClient>>>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _sut = new LMStudioVisionClient(_loggerMock.Object, _httpClient);
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
            DefaultVisionModel,
            DefaultLMStudioUrl);

        // Assert
        Assert.That(result, Is.EqualTo("Extracted text from image"));
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
            DefaultVisionModel,
            DefaultLMStudioUrl);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ExtractTextFromImageAsync_HttpError_ThrowsHttpRequestException()
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
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExtractTextFromImageAsync(
            imagePath,
            DefaultVisionModel,
            DefaultLMStudioUrl));
    }

    /// <summary>
    /// Integration test using real HttpClient and real LM Studio instance.
    /// This is a simple, standalone test that uses a test image file.
    /// Requires LM Studio to be running at http://localhost:1234 with qwen/qwen2.5-vl-7b model loaded.
    ///
    /// To run manually:
    /// 1. Start LM Studio and load the qwen/qwen2.5-vl-7b model
    /// 2. Run: dotnet test --filter "FullyQualifiedName~ExtractTextFromImageAsync_RealLMStudioWithTestImage"
    /// </summary>
    [Test]
    [Explicit("Requires real LM Studio instance running")]
    public async Task ExtractTextFromImageAsync_RealLMStudioWithTestImage_ExtractsText()
    {
        // Arrange - Create a simple test image with text
        var testImagePath = GetTestTempFilePath();

        try
        {
            // Create a simple 200x100 PNG image with white background and black text
            using (var bitmap = new System.Drawing.Bitmap(200, 100))
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.White);
                graphics.DrawString(
                    "Hello World\nTest Image",
                    new System.Drawing.Font("Arial", 12),
                    System.Drawing.Brushes.Black,
                    new System.Drawing.PointF(10, 10));

                bitmap.Save(testImagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            Assert.That(File.Exists(testImagePath), Is.True, "Test image should be created");

            var loggerMock = new Mock<ILogger<LMStudioVisionClient>>();
            var realHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var client = new LMStudioVisionClient(loggerMock.Object, realHttpClient);

            // Act - Extract text using real LM Studio
            TestContext.WriteLine($"Sending image to LM Studio at {DefaultLMStudioUrl}...");
            TestContext.WriteLine($"Using model: {DefaultVisionModel}");

            var result = await client.ExtractTextFromImageAsync(
                testImagePath,
                DefaultVisionModel,
                DefaultLMStudioUrl);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Not.Empty, "Result should not be empty");

            // Output the extracted text for manual verification
            TestContext.WriteLine($"\n--- Extracted Text ({result.Length} characters) ---");
            TestContext.WriteLine(result);
            TestContext.WriteLine("--- End of Extracted Text ---");

            realHttpClient.Dispose();
        }
        finally
        {
            // Cleanup
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
            }
        }
    }

    /// <summary>
    /// Integration test using real HttpClient and real LM Studio instance with a rasterized PDF page.
    /// Requires LM Studio to be running at http://localhost:1234 with qwen/qwen2.5-vl-7b model loaded.
    /// Run manually with: dotnet test --filter "FullyQualifiedName~ExtractTextFromImageAsync_RealLMStudioWithPdf"
    /// </summary>
    [Test]
    [Explicit("Requires real LM Studio instance running")]
    public async Task ExtractTextFromImageAsync_RealLMStudioWithPdf_ExtractsText()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "SemanticKernel-FundDocsQnA-dotnet-nextjs",
            "PdfTextExtractor",
            "temp");

        var outputDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        var loggerMock = new Mock<ILogger<LMStudioVisionClient>>();
        var realHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var client = new LMStudioVisionClient(loggerMock.Object, realHttpClient);

        // First, rasterize page 1 of the test PDF to an image
        var rasterizer = new Mock<ILogger<PdfPageRasterizer>>();
        var pageRasterizer = new PdfPageRasterizer(rasterizer.Object);
        var eventPublisher = new Mock<PdfTextExtractor.Core.Domain.Events.IEventPublisher>();

        string? imagePath = null;

        try
        {
            // Rasterize first page
            TestContext.WriteLine("Rasterizing page 1 of test PDF...");
            var rasterizationResult = await pageRasterizer.RasterizePageAsync(
                TestPdfFiles.SamplePdf,
                pageNumber: 1,
                outputDir,
                dpi: 150,
                eventPublisher.Object,
                Guid.NewGuid(),
                Guid.NewGuid());

            imagePath = rasterizationResult.TempImagePath;

            Assert.That(File.Exists(imagePath), Is.True, "Rasterized image should exist");
            TestContext.WriteLine($"Image created: {imagePath} ({rasterizationResult.ImageSizeBytes} bytes)");

            // Act - Extract text using real LM Studio
            TestContext.WriteLine($"\nSending image to LM Studio at {DefaultLMStudioUrl}...");
            TestContext.WriteLine($"Using model: {DefaultVisionModel}");

            var result = await client.ExtractTextFromImageAsync(
                imagePath,
                DefaultVisionModel,
                DefaultLMStudioUrl);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Not.Empty, "Result should not be empty");

            // Output the extracted text for manual verification
            TestContext.WriteLine($"\n--- Extracted Text ({result.Length} characters) ---");
            TestContext.WriteLine(result.Length > 500
                ? result.Substring(0, 500) + "\n... (truncated)"
                : result);
            TestContext.WriteLine("--- End of Extracted Text ---");
        }
        finally
        {
            // Cleanup
            if (imagePath != null && File.Exists(imagePath))
            {
                File.Delete(imagePath);
            }

            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }

            realHttpClient.Dispose();
        }
    }

    /// <summary>
    /// Simple integration test with real HttpClient - use this to manually test LM Studio connection.
    /// Requires: LM Studio running at http://localhost:1234 with qwen/qwen2.5-vl-7b model loaded.
    ///
    /// Run with: dotnet test --filter "FullyQualifiedName~RealHttpClient"
    /// </summary>
    [Test]
    [Explicit("Manual integration test - requires LM Studio running")]
    public async Task ExtractTextFromImageAsync_RealHttpClient_WithSimpleImage()
    {
        // Arrange - Create simple test image
        var imagePath = GetTestTempFilePath();

        try
        {
            // Create a 300x200 white image with black text
            using (var bitmap = new System.Drawing.Bitmap(300, 200))
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.White);
                var font = new System.Drawing.Font("Arial", 16, System.Drawing.FontStyle.Bold);
                graphics.DrawString("HELLO WORLD", font, System.Drawing.Brushes.Black, 50, 80);
                bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            // Create client with real HttpClient
            var logger = new Mock<ILogger<LMStudioVisionClient>>().Object;
            var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var client = new LMStudioVisionClient(logger, httpClient);

            TestContext.WriteLine($"Test image: {imagePath}");
            TestContext.WriteLine($"LM Studio URL: {DefaultLMStudioUrl}");
            TestContext.WriteLine($"Model: {DefaultVisionModel}");
            TestContext.WriteLine("\nSending request to LM Studio...\n");

            // Act
            var result = await client.ExtractTextFromImageAsync(
                imagePath,
                DefaultVisionModel,
                DefaultLMStudioUrl);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);

            TestContext.WriteLine($"SUCCESS! Extracted text ({result.Length} chars):");
            TestContext.WriteLine("---");
            TestContext.WriteLine(result);
            TestContext.WriteLine("---");

            httpClient.Dispose();
        }
        finally
        {
            if (File.Exists(imagePath))
            {
                File.Delete(imagePath);
            }
        }
    }
}
