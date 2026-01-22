using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Infrastructure.Extractors;
using PdfTextExtractor.Core.Tests.AutoFixture;
using PdfTextExtractor.Core.Tests.TestHelpers;

namespace PdfTextExtractor.Core.Tests.Infrastructure.Extractors;

[TestFixture]
[TestOf(typeof(PdfPigExtractor))]
public class PdfPigExtractorTests
{
    private IFixture _fixture;
    private Mock<IEventPublisher> _eventPublisherMock;
    private Mock<ILogger<PdfPigExtractor>> _loggerMock;
    private PdfPigExtractor _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _eventPublisherMock = _fixture.Freeze<Mock<IEventPublisher>>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<PdfPigExtractor>>>();
        _sut = new PdfPigExtractor(_loggerMock.Object);
    }

    [Test]
    public void ExtractAsync_ValidParameters_CreatesExtractor()
    {
        // Arrange
        var loggerMock = _fixture.Create<Mock<ILogger<PdfPigExtractor>>>();

        // Act
        var extractor = new PdfPigExtractor(loggerMock.Object);

        // Assert
        Assert.That(extractor, Is.Not.Null);
    }

    [Test]
    public async Task ExtractAsync_RealPdf_ExtractsTextSuccessfully()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        // Act
        var result = await _sut.ExtractAsync(
            TestPdfFiles.SamplePdf,
            _eventPublisherMock.Object,
            correlationId,
            sessionId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.All(c => !string.IsNullOrWhiteSpace(c.PageText)), Is.True);

        // Verify DocumentExtractionStarted event was published
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<PdfExtractionEventBase>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
