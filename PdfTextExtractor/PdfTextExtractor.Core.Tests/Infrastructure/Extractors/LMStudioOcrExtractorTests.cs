using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Infrastructure.Extractors;
using PdfTextExtractor.Core.Infrastructure.LMStudio;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Models;
using PdfTextExtractor.Core.Tests.AutoFixture;
using PdfTextExtractor.Core.Tests.TestHelpers;

namespace PdfTextExtractor.Core.Tests.Infrastructure.Extractors;

[TestFixture]
[TestOf(typeof(LMStudioOcrExtractor))]
public class LMStudioOcrExtractorTests
{
    private IFixture _fixture;
    private Mock<IRasterizationService> _rasterizationServiceMock;
    private Mock<ILMStudioVisionClient> _visionClientMock;
    private Mock<IEventPublisher> _eventPublisherMock;
    private LMStudioParameters _parameters;
    private LMStudioOcrExtractor _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _rasterizationServiceMock = _fixture.Freeze<Mock<IRasterizationService>>();
        _visionClientMock = _fixture.Freeze<Mock<ILMStudioVisionClient>>();
        _eventPublisherMock = _fixture.Freeze<Mock<IEventPublisher>>();

        _parameters = _fixture.Create<LMStudioParameters>();
        _sut = _fixture.Create<LMStudioOcrExtractor>();
    }

    [Test]
    public async Task ExtractAsync_ValidPdf_CallsRasterizationService()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var tempImagePath = Path.Combine(Path.GetTempPath(), "temp.png");
        await File.WriteAllBytesAsync(tempImagePath, new byte[] { 1, 2, 3 });

        _rasterizationServiceMock
            .Setup(x => x.RasterizePageAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<IEventPublisher>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1000 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisionExtractionResult { ExtractedText = "Extracted text from page" });

        try
        {
            // Act
            var result = await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

            // Assert
            _rasterizationServiceMock.Verify(
                x => x.RasterizePageAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<IEventPublisher>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
        finally
        {
            if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
        }
    }

    [Test]
    public async Task ExtractAsync_ValidPdf_CallsVisionClient()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var tempImagePath = Path.Combine(Path.GetTempPath(), "temp.png");
        await File.WriteAllBytesAsync(tempImagePath, new byte[] { 1, 2, 3 });

        _rasterizationServiceMock
            .Setup(x => x.RasterizePageAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<IEventPublisher>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1000 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisionExtractionResult { ExtractedText = "Extracted text from page" });

        try
        {
            // Act
            var result = await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

            // Assert
            _visionClientMock.Verify(
                x => x.ExtractTextFromImageAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
        finally
        {
            if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
        }
    }

    [Test]
    public async Task ExtractAsync_EmptyResponse_HandlesGracefully()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var tempImagePath = Path.Combine(Path.GetTempPath(), "temp.png");
        await File.WriteAllBytesAsync(tempImagePath, new byte[] { 1, 2, 3 });

        _rasterizationServiceMock
            .Setup(x => x.RasterizePageAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<IEventPublisher>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1000 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisionExtractionResult { ExtractedText = string.Empty });

        try
        {
            // Act
            var result = await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }
        finally
        {
            if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
        }
    }
}
