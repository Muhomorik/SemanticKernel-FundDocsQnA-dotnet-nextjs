using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.ValueObjects;

[TestFixture]
[TestOf(typeof(ExtractorType))]
public class ExtractorTypeTests
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
    public void FromString_PdfPig_ReturnsPdfPigType()
    {
        // Arrange & Act
        var result = ExtractorType.FromString("PdfPig");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo("PdfPig"));
    }

    [Test]
    public void FromString_LMStudio_ReturnsLMStudioType()
    {
        // Arrange & Act
        var result = ExtractorType.FromString("LMStudio");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo("LMStudio"));
    }
}
