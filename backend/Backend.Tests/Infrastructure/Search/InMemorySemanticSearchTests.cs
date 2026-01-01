using AutoFixture;
using AutoFixture.AutoMoq;

using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;
using Backend.API.Domain.ValueObjects;
using Backend.API.Infrastructure.Search;
using Backend.Tests.TestInfrastructure;

using Microsoft.Extensions.Logging;

using Moq;

namespace Backend.Tests.Infrastructure.Search;

[TestFixture]
public class InMemorySemanticSearchTests
{
    private IFixture _fixture;
    private Mock<IDocumentRepository> _repositoryMock;
    private Mock<IEmbeddingGenerator> _embeddingGeneratorMock;
    private Mock<ILogger<InMemorySemanticSearch>> _loggerMock;
    private InMemorySemanticSearch _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new BackendDomainCustomization());

        _repositoryMock = _fixture.Freeze<Mock<IDocumentRepository>>();
        _embeddingGeneratorMock = _fixture.Freeze<Mock<IEmbeddingGenerator>>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<InMemorySemanticSearch>>>();
        _sut = _fixture.Create<InMemorySemanticSearch>();
    }

    [Test]
    public void SearchAsync_NegativeCosineSimilarity_ThrowsArgumentOutOfRangeException()
    {
        // Use two vectors that are exact opposites to guarantee cosine similarity of -1
        var queryEmbedding = new float[] { 1.0f, 0.0f };
        var chunkEmbedding = new float[] { -1.0f, 0.0f };
        var query = "test";
        var queryVector = new EmbeddingVector(queryEmbedding);
        var chunk = DocumentChunk.Create("1", "text", chunkEmbedding, "source", 1);

        _embeddingGeneratorMock
            .Setup(x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        _repositoryMock
            .Setup(x => x.GetAllChunksAsync())
            .ReturnsAsync(new List<DocumentChunk> { chunk });

        // Act
        // With correct normalization, similarity should be 0.0 (not exception)
        var resultsTask = _sut.SearchAsync(query, 1, CancellationToken.None);
        // Assert
        Assert.That(async () => await resultsTask, Throws.Nothing,
            "Should not throw for negative cosine similarity after normalization");
        if (resultsTask.IsCompletedSuccessfully)
        {
            var results = resultsTask.Result;
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].SimilarityScore, Is.EqualTo(0.0f).Within(0.0001f));
        }
    }
}