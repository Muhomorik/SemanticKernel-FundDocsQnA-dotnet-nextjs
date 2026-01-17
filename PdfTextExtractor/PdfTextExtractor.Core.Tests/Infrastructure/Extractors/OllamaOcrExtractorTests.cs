using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Infrastructure.Extractors;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Infrastructure.Extractors;

[TestFixture]
public class OllamaOcrExtractorTests
{
    private IFixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());
    }

    [Test]
    public void ExtractAsync_AnyInput_ThrowsNotImplementedException()
    {
        // Arrange
        var sut = _fixture.Create<OllamaOcrExtractor>();
        var pdfPath = "test.pdf";
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        // Act & Assert
        Assert.ThrowsAsync<NotImplementedException>(
            async () => await sut.ExtractAsync(pdfPath, null!, correlationId, sessionId));
    }
}
