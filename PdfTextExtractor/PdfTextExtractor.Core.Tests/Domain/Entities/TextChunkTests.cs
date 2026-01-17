using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.Entities;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.Entities;

[TestFixture]
[TestOf(typeof(TextChunk))]
public class TextChunkTests
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
    public void Create_ValidParameters_ReturnsTextChunk()
    {
        // Arrange
        var pageNumber = _fixture.Create<PageNumber>();
        var chunkIndex = 5;
        var content = _fixture.Create<ChunkContent>();

        // Act
        var result = TextChunk.Create(pageNumber, chunkIndex, content);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Create_ValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var pageNumber = PageNumber.Create(10);
        var chunkIndex = 5;
        var content = ChunkContent.Create("Test content");

        // Act
        var result = TextChunk.Create(pageNumber, chunkIndex, content);

        // Assert
        Assert.That(result.PageNumber, Is.EqualTo(pageNumber));
        Assert.That(result.ChunkIndex, Is.EqualTo(chunkIndex));
        Assert.That(result.Content, Is.EqualTo(content));
    }

    [Test]
    public void Create_ValidParameters_InitializesCreatedAt()
    {
        // Arrange
        var pageNumber = _fixture.Create<PageNumber>();
        var chunkIndex = 1;
        var content = _fixture.Create<ChunkContent>();

        // Act
        var result = TextChunk.Create(pageNumber, chunkIndex, content);

        // Assert
        Assert.That(result.CreatedAt, Is.Not.EqualTo(DateTime.MinValue));
        Assert.That(result.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public void ChunkId_ValidTextChunk_ReturnsUniqueId()
    {
        // Arrange
        var textChunk1 = _fixture.Create<TextChunk>();
        var textChunk2 = _fixture.Create<TextChunk>();

        // Act
        var id1 = textChunk1.ChunkId;
        var id2 = textChunk2.ChunkId;

        // Assert
        Assert.That(id1, Is.Not.EqualTo(Guid.Empty));
        Assert.That(id2, Is.Not.EqualTo(Guid.Empty));
        Assert.That(id1, Is.Not.EqualTo(id2));
    }
}
