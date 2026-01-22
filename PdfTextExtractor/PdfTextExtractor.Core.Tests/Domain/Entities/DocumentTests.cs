using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.Entities;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.Entities;

[TestFixture]
[TestOf(typeof(Document))]
public class DocumentTests
{
    private IFixture _fixture;
    private FilePath _filePath;
    private CorrelationId _correlationId;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _filePath = _fixture.Create<FilePath>();
        _correlationId = _fixture.Create<CorrelationId>();
    }

    [Test]
    public void Create_ValidParameters_ReturnsDocument()
    {
        // Arrange
        var fileSizeBytes = _fixture.Create<long>();

        // Act
        var result = Document.Create(_filePath, fileSizeBytes, _correlationId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DocumentId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Create_ValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var fileSizeBytes = 12345L;

        // Act
        var result = Document.Create(_filePath, fileSizeBytes, _correlationId);

        // Assert
        Assert.That(result.FilePath, Is.EqualTo(_filePath));
        Assert.That(result.FileSizeBytes, Is.EqualTo(fileSizeBytes));
        Assert.That(result.CorrelationId, Is.EqualTo(_correlationId));
        Assert.That(result.IsCompleted, Is.False);
    }

    [Test]
    public void AddPage_ValidPageNumber_AddsPageToCollection()
    {
        // Arrange
        var document = _fixture.Create<Document>();
        var pageNumber = _fixture.Create<PageNumber>();

        // Act
        var page = document.AddPage(pageNumber);

        // Assert
        Assert.That(document.Pages, Contains.Item(page));
        Assert.That(document.TotalPages, Is.EqualTo(1));
    }

    [Test]
    public void MarkAsCompleted_NotCompletedDocument_SetsCompletedAt()
    {
        // Arrange
        var document = _fixture.Create<Document>();
        Assert.That(document.IsCompleted, Is.False);

        // Act
        document.MarkAsCompleted();

        // Assert
        Assert.That(document.IsCompleted, Is.True);
        Assert.That(document.CompletedAt, Is.Not.Null);
    }

    [Test]
    public void Duration_CompletedDocument_ReturnsTimeSpan()
    {
        // Arrange
        var document = _fixture.Create<Document>();
        document.MarkAsCompleted();

        // Act
        var duration = document.Duration;

        // Assert
        Assert.That(duration, Is.Not.Null);
        Assert.That(duration.Value.TotalMilliseconds, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void TotalPages_DocumentWithPages_ReturnsCorrectCount()
    {
        // Arrange
        var document = _fixture.Create<Document>();
        document.AddPage(PageNumber.Create(1));
        document.AddPage(PageNumber.Create(2));
        document.AddPage(PageNumber.Create(3));

        // Act
        var totalPages = document.TotalPages;

        // Assert
        Assert.That(totalPages, Is.EqualTo(3));
    }

}
