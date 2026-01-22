using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.Entities;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.Entities;

[TestFixture]
[TestOf(typeof(Page))]
public class PageTests
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
    public void Create_ValidPageNumber_ReturnsPage()
    {
        // Arrange
        var pageNumber = _fixture.Create<PageNumber>();

        // Act
        var result = Page.Create(pageNumber);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Create_ValidPageNumber_SetsPropertiesCorrectly()
    {
        // Arrange
        var pageNumber = PageNumber.Create(5);

        // Act
        var result = Page.Create(pageNumber);

        // Assert
        Assert.That(result.PageNumber, Is.EqualTo(pageNumber));
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void MarkAsEmpty_ValidPage_SetsIsEmptyFlag()
    {
        // Arrange
        var page = _fixture.Create<Page>();
        Assert.That(page.IsEmpty, Is.False);

        // Act
        page.MarkAsEmpty();

        // Assert
        Assert.That(page.IsEmpty, Is.True);
    }

    [Test]
    public void MarkAsProcessed_ValidPage_SetsProcessedAt()
    {
        // Arrange
        var page = _fixture.Create<Page>();
        Assert.That(page.ProcessedAt, Is.Null);

        // Act
        page.MarkAsProcessed();

        // Assert
        Assert.That(page.ProcessedAt, Is.Not.Null);
        Assert.That(page.ProcessedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }
}
