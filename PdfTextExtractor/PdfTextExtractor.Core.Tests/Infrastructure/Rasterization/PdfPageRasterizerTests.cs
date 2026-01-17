using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Tests.AutoFixture;
using PdfTextExtractor.Core.Tests.TestHelpers;

namespace PdfTextExtractor.Core.Tests.Infrastructure.Rasterization;

[TestFixture]
[TestOf(typeof(PdfPageRasterizer))]
public class PdfPageRasterizerTests
{
    private IFixture _fixture;
    private Mock<IEventPublisher> _eventPublisherMock;
    private Mock<ILogger<PdfPageRasterizer>> _loggerMock;
    private PdfPageRasterizer _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _eventPublisherMock = _fixture.Freeze<Mock<IEventPublisher>>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<PdfPageRasterizer>>>();
        _sut = _fixture.Create<PdfPageRasterizer>();
    }

    [Test]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var loggerMock = _fixture.Create<Mock<ILogger<PdfPageRasterizer>>>();

        // Act
        var rasterizer = new PdfPageRasterizer(loggerMock.Object);

        // Assert
        Assert.That(rasterizer, Is.Not.Null);
    }

    [Test]
    public async Task RasterizePageAsync_RealPdf_CreatesImageFile()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        try
        {
            // Act
            var result = await _sut.RasterizePageAsync(
                TestPdfFiles.SamplePdf,
                pageNumber: 1,
                outputDir,
                dpi: 150,
                _eventPublisherMock.Object,
                correlationId,
                sessionId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TempImagePath, Is.Not.Null);
            Assert.That(File.Exists(result.TempImagePath), Is.True);
            Assert.That(result.ImageSizeBytes, Is.GreaterThan(0));

            // Verify events were published
            _eventPublisherMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<PdfExtractionEventBase>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }
}
