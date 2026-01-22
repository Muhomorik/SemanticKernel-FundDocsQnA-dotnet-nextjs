using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Document;
using PdfTextExtractor.Core.Domain.Events.Infrastructure;
using PdfTextExtractor.Core.Domain.Events.Ocr;
using PdfTextExtractor.Core.Domain.Events.Page;
using PdfTextExtractor.Core.Infrastructure.Extractors;
using PdfTextExtractor.Core.Infrastructure.LMStudio;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Models;
using PdfTextExtractor.Core.Tests.AutoFixture;
using PdfTextExtractor.Core.Tests.TestHelpers;

namespace PdfTextExtractor.Core.Tests.Infrastructure.Extractors;

/// <summary>
/// Comprehensive unit tests for LMStudioOcrExtractor covering all configuration options,
/// scenarios, and code paths.
/// </summary>
[TestFixture]
[TestOf(typeof(LMStudioOcrExtractor))]
public class LMStudioOcrExtractorComprehensiveTests
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

    #region Property Tests

    [Test]
    public void Method_ReturnsLMStudioExtractionMethod()
    {
        // Act
        var method = _sut.Method;

        // Assert
        Assert.That(method, Is.EqualTo(TextExtractionMethod.LMStudio));
    }

    #endregion

    #region Single Page Extraction Tests

    [Test]
    public async Task ExtractAsync_SinglePagePdf_ExtractsTextSuccessfully()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var extractedText = _fixture.Create<string>();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

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
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1024 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        // Act
        var result = await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.All(c => c.SourceFile == pdfPath), Is.True);
        Assert.That(result.All(c => c.PageNumber > 0), Is.True);
    }

    [Test]
    public async Task ExtractAsync_SinglePage_PublishesDocumentStartedEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<DocumentExtractionStarted>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId &&
                    e.ExtractorName == "LMStudio" &&
                    e.FilePath == pdfPath),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractAsync_SinglePage_PublishesDocumentCompletedEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<DocumentExtractionCompleted>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId &&
                    e.ExtractorName == "LMStudio"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractAsync_SinglePage_PublishesPageStartedEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<PageExtractionStarted>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId &&
                    e.PageNumber > 0),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ExtractAsync_SinglePage_PublishesPageCompletedEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<PageExtractionCompleted>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Rasterization Tests

    [Test]
    public async Task ExtractAsync_CallsRasterizationWithCorrectDpi()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var customDpi = 600;
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        _parameters = new LMStudioParameters
        {
            PdfFolderPath = _fixture.Create<string>(),
            OutputFolderPath = _fixture.Create<string>(),
            RasterizationDpi = customDpi,
        };
        _sut = _fixture.Create<LMStudioOcrExtractor>();

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _rasterizationServiceMock.Verify(
            x => x.RasterizePageAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                customDpi,
                It.IsAny<IEventPublisher>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ExtractAsync_CallsRasterizationWithDefaultDpi()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _rasterizationServiceMock.Verify(
            x => x.RasterizePageAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                _parameters.RasterizationDpi,
                It.IsAny<IEventPublisher>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ExtractAsync_RasterizationCreatesTemporaryImage()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _visionClientMock.Verify(
            x => x.ExtractTextFromImageAsync(
                tempImagePath,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region OCR Processing Tests

    [Test]
    public async Task ExtractAsync_PublishesOcrStartedEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<OcrProcessingStarted>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId &&
                    e.VisionModelName == _parameters.VisionModelName),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ExtractAsync_PublishesOcrCompletedEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<OcrProcessingCompleted>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ExtractAsync_CallsVisionClientWithCorrectModelName()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var customModel = "custom-vision-model";
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        _parameters = new LMStudioParameters
        {
            PdfFolderPath = _fixture.Create<string>(),
            OutputFolderPath = _fixture.Create<string>(),
            VisionModelName = customModel
        };
        _sut = _fixture.Create<LMStudioOcrExtractor>();

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _visionClientMock.Verify(
            x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                customModel,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ExtractAsync_CallsVisionClientWithCorrectUrl()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var customUrl = "http://custom-lmstudio:8080";
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        _parameters = new LMStudioParameters
        {
            PdfFolderPath = _fixture.Create<string>(),
            OutputFolderPath = _fixture.Create<string>(),
            LMStudioUrl = customUrl
        };
        _sut = _fixture.Create<LMStudioOcrExtractor>();

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _visionClientMock.Verify(
            x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                customUrl,
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Empty Page Handling Tests

    [Test]
    public async Task ExtractAsync_EmptyText_PublishesEmptyPageDetectedEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

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
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1024 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmptyPageDetected>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ExtractAsync_WhitespaceOnlyText_PublishesEmptyPageDetectedEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

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
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1024 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("   \t\n\r   ");

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmptyPageDetected>(e => e.CorrelationId == correlationId),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ExtractAsync_EmptyPage_DoesNotCreateChunks()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

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
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1024 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Progress Tracking Tests

    [Test]
    public async Task ExtractAsync_PublishesProgressUpdatedEvents()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<ExtractionProgressUpdated>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId &&
                    e.OverallPercentage >= 0 &&
                    e.OverallPercentage <= 100),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Cleanup Tests

    [Test]
    public async Task ExtractAsync_PublishesTempFilesCleanedUpEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        SetupSuccessfulExtraction(tempImagePath);

        // Act
        await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<TempFilesCleanedUp>(e =>
                    e.CorrelationId == correlationId &&
                    e.SessionId == sessionId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Cancellation Tests

    [Test]
    public async Task ExtractAsync_CancellationRequested_PublishesCancellationEvent()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var pdfPath = TestPdfFiles.SamplePdf;
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

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
            .Callback(() => cts.Cancel())
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1024 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<string>());

        // Act & Assert
        try
        {
            await _sut.ExtractAsync(pdfPath, _eventPublisherMock.Object, correlationId, sessionId, cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            _eventPublisherMock.Verify(
                x => x.PublishAsync(
                    It.Is<DocumentExtractionCancelled>(e =>
                        e.CorrelationId == correlationId &&
                        e.SessionId == sessionId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulExtraction(string tempImagePath, string? extractedText = null)
    {
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
            .ReturnsAsync(new RasterizationResult { TempImagePath = tempImagePath, ImageSizeBytes = 1024 });

        _visionClientMock
            .Setup(x => x.ExtractTextFromImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText ?? _fixture.Create<string>());
    }

    #endregion
}
