using Backend.API.Domain.Models;
using Backend.API.Infrastructure.Search.Mapping;
using Backend.API.Infrastructure.Search.Models;

namespace Backend.Tests.Infrastructure.Search.Mapping;

[TestFixture]
public class DocumentChunkMapperTests
{
    private const int VectorDimensions = 1536;

    [Test]
    public void ToRecord_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var embedding = CreateTestVector();
        var chunk = DocumentChunk.Create("test-id", "test text", embedding, "source.pdf", 5);

        // Act
        var record = DocumentChunkMapper.ToRecord(chunk);

        // Assert
        Assert.That(record.Id, Is.EqualTo("test-id"));
        Assert.That(record.Text, Is.EqualTo("test text"));
        Assert.That(record.Source, Is.EqualTo("source.pdf"));
        Assert.That(record.Page, Is.EqualTo(5));
        Assert.That(record.Vector.ToArray(), Is.EqualTo(embedding));
    }

    [Test]
    public void ToDomain_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var embedding = CreateTestVector();
        var record = new DocumentChunkRecord
        {
            Id = "record-id",
            Text = "record text",
            Source = "record.pdf",
            Page = 10,
            Vector = new ReadOnlyMemory<float>(embedding)
        };

        // Act
        var chunk = DocumentChunkMapper.ToDomain(record);

        // Assert
        Assert.That(chunk.Id, Is.EqualTo("record-id"));
        Assert.That(chunk.Text, Is.EqualTo("record text"));
        Assert.That(chunk.Metadata.Source, Is.EqualTo("record.pdf"));
        Assert.That(chunk.Metadata.Page, Is.EqualTo(10));
        Assert.That(chunk.Vector.Values, Is.EqualTo(embedding));
    }

    [Test]
    public void RoundTrip_PreservesAllData()
    {
        // Arrange
        var originalEmbedding = CreateTestVector();
        var originalChunk = DocumentChunk.Create("roundtrip-id", "roundtrip text", originalEmbedding, "roundtrip.pdf", 7);

        // Act - Convert to record and back
        var record = DocumentChunkMapper.ToRecord(originalChunk);
        var resultChunk = DocumentChunkMapper.ToDomain(record);

        // Assert
        Assert.That(resultChunk.Id, Is.EqualTo(originalChunk.Id));
        Assert.That(resultChunk.Text, Is.EqualTo(originalChunk.Text));
        Assert.That(resultChunk.Metadata.Source, Is.EqualTo(originalChunk.Metadata.Source));
        Assert.That(resultChunk.Metadata.Page, Is.EqualTo(originalChunk.Metadata.Page));
        Assert.That(resultChunk.Vector.Values, Is.EqualTo(originalChunk.Vector.Values));
    }

    [Test]
    public void ToRecord_PreservesVectorDimensions()
    {
        // Arrange
        var embedding = CreateTestVector();
        var chunk = DocumentChunk.Create("id", "text", embedding, "source.pdf", 1);

        // Act
        var record = DocumentChunkMapper.ToRecord(chunk);

        // Assert
        Assert.That(record.Vector.Length, Is.EqualTo(VectorDimensions));
    }

    private static float[] CreateTestVector()
    {
        return Enumerable.Range(0, VectorDimensions)
            .Select(i => (float)i / VectorDimensions)
            .ToArray();
    }
}
