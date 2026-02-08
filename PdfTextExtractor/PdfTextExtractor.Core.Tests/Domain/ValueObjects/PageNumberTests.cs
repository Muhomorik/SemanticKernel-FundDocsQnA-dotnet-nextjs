using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.ValueObjects;

[TestFixture]
[TestOf(typeof(PageNumber))]
public class PageNumberTests
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
    public void Create_ValidPageNumber_ReturnsPageNumber()
    {
        // Arrange
        var validPageNumber = 5;

        // Act
        var result = PageNumber.Create(validPageNumber);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Create_ValidPageNumber_SetsValueCorrectly()
    {
        // Arrange
        var validPageNumber = 10;

        // Act
        var result = PageNumber.Create(validPageNumber);

        // Assert
        Assert.That(result.Value, Is.EqualTo(validPageNumber));
    }

    [Test]
    public void ImplicitOperator_ValidPageNumber_ConvertsToInt()
    {
        // Arrange
        var pageNumber = PageNumber.Create(15);

        // Act
        int value = pageNumber;

        // Assert
        Assert.That(value, Is.EqualTo(15));
    }

    [Test]
    public void Value_ValidPageNumber_ReturnsCorrectValue()
    {
        // Arrange
        var expected = 42;
        var pageNumber = PageNumber.Create(expected);

        // Act
        var actual = pageNumber.Value;

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }
}
