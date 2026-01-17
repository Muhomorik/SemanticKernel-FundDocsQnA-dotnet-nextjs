using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.ValueObjects;

[TestFixture]
[TestOf(typeof(ChunkContent))]
public class ChunkContentTests
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
    public void Create_ValidContent_ReturnsChunkContent()
    {
        // Arrange
        var validContent = "This is a valid chunk content";

        // Act
        var result = ChunkContent.Create(validContent);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Create_ValidContent_SetsValueCorrectly()
    {
        // Arrange
        var validContent = "Sample text content";

        // Act
        var result = ChunkContent.Create(validContent);

        // Assert
        Assert.That(result.Value, Is.EqualTo(validContent));
    }

    [Test]
    public void Length_ValidContent_ReturnsCorrectLength()
    {
        // Arrange
        var content = "Hello World";
        var chunkContent = ChunkContent.Create(content);

        // Act
        var length = chunkContent.Length;

        // Assert
        Assert.That(length, Is.EqualTo(11));
    }

    [Test]
    public void Preview_LongContent_TruncatesCorrectly()
    {
        // Arrange
        var longContent = new string('A', 200);
        var chunkContent = ChunkContent.Create(longContent);
        var maxLength = 50;

        // Act
        var preview = chunkContent.Preview(maxLength);

        // Assert
        Assert.That(preview.Length, Is.LessThanOrEqualTo(maxLength + 3)); // +3 for "..."
        Assert.That(preview, Does.EndWith("..."));
    }
}
