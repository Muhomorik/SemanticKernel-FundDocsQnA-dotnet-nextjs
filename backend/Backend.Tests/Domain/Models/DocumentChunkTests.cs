using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.Domain.Models;
using Backend.Tests.TestInfrastructure;
using NUnit.Framework;

namespace Backend.Tests.Domain.Models;

[TestFixture]
public class DocumentChunkTests
{
    private IFixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new BackendDomainCustomization());
    }

    [Test]
    public void Create_ValidParameters_CreatesInstance()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var text = "Sample text content";
        var embedding = Enumerable.Range(0, 1536).Select(i => (float)i).ToArray();
        var source = "test-document.pdf";
        var page = 1;

        // Act
        var result = DocumentChunk.Create(id, text, embedding, source, page);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(id));
        Assert.That(result.Text, Is.EqualTo(text));
    }

    [Test]
    public void Create_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var text = "Sample text content";
        var embedding = Enumerable.Range(0, 1536).Select(i => (float)i).ToArray();
        var source = "test-document.pdf";
        var page = 42;

        // Act
        var result = DocumentChunk.Create(id, text, embedding, source, page);

        // Assert
        Assert.That(result.Id, Is.EqualTo(id));
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.Vector.Values, Is.EqualTo(embedding));
        Assert.That(result.Metadata.Source, Is.EqualTo(source));
        Assert.That(result.Metadata.Page, Is.EqualTo(page));
    }

    [Test]
    public void Create_InitializesVectorAndMetadata()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var text = "Sample text content";
        var embedding = Enumerable.Range(0, 1536).Select(i => (float)i).ToArray();
        var source = "test-document.pdf";
        var page = 1;

        // Act
        var result = DocumentChunk.Create(id, text, embedding, source, page);

        // Assert
        Assert.That(result.Vector, Is.Not.Null);
        Assert.That(result.Metadata, Is.Not.Null);
        Assert.That(result.Vector.Dimensions, Is.EqualTo(1536));
        Assert.That(result.Metadata.Source, Is.EqualTo(source));
    }
}
