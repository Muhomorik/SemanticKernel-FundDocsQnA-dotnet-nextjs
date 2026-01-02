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

    private const int VectorDimensions = 1536;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new BackendDomainCustomization());

        _repositoryMock = _fixture.Freeze<Mock<IDocumentRepository>>();
        _embeddingGeneratorMock = _fixture.Freeze<Mock<IEmbeddingGenerator>>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<InMemorySemanticSearch>>>();

        // Create a fresh SUT for each test to avoid VectorStore state leaking between tests
        _sut = new InMemorySemanticSearch(
            _repositoryMock.Object,
            _embeddingGeneratorMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task SearchAsync_WithMatchingChunks_ReturnsResultsWithNormalizedScores()
    {
        // Arrange - Create vectors that are similar (same direction)
        var embedding = CreateNormalizedVector(1.0f);
        var queryVector = new EmbeddingVector(embedding);
        var chunk = DocumentChunk.Create("1", "matching text", embedding, "source.pdf", 1);

        _repositoryMock
            .Setup(x => x.GetAllChunksAsync())
            .ReturnsAsync(new List<DocumentChunk> { chunk });

        _embeddingGeneratorMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        // Act
        var results = await _sut.SearchAsync("test query", 10, CancellationToken.None);

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].SimilarityScore, Is.InRange(0.0f, 1.0f));
        Assert.That(results[0].Chunk.Id, Is.EqualTo("1"));
    }

    [Test]
    public async Task SearchAsync_NegativeCosineSimilarity_ReturnsNormalizedScoreNearZero()
    {
        // Arrange - Use opposite vectors to get cosine similarity of -1
        var queryEmbedding = CreateNormalizedVector(1.0f);
        var chunkEmbedding = CreateNormalizedVector(-1.0f); // Opposite direction
        var queryVector = new EmbeddingVector(queryEmbedding);
        var chunk = DocumentChunk.Create("1", "opposite text", chunkEmbedding, "source.pdf", 1);

        _repositoryMock
            .Setup(x => x.GetAllChunksAsync())
            .ReturnsAsync(new List<DocumentChunk> { chunk });

        _embeddingGeneratorMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        // Act
        var results = await _sut.SearchAsync("test", 1, CancellationToken.None);

        // Assert - Score should be normalized to [0,1], near 0 for opposite vectors
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].SimilarityScore, Is.InRange(0.0f, 1.0f),
            "Score should be normalized to [0,1] range");
        Assert.That(results[0].SimilarityScore, Is.EqualTo(0.0f).Within(0.01f),
            "Opposite vectors should have score near 0 after normalization");
    }

    [Test]
    public async Task SearchAsync_WithEmptyRepository_ReturnsEmptyResults()
    {
        // Arrange
        var queryVector = new EmbeddingVector(new float[VectorDimensions]);

        _repositoryMock
            .Setup(x => x.GetAllChunksAsync())
            .ReturnsAsync(new List<DocumentChunk>());

        _embeddingGeneratorMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        // Act
        var results = await _sut.SearchAsync("test", 10, CancellationToken.None);

        // Assert
        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_RespectsMaxResultsLimit()
    {
        // Arrange
        var chunks = Enumerable.Range(1, 10)
            .Select(i => DocumentChunk.Create(
                $"id-{i}",
                $"text-{i}",
                CreateRandomVector(),
                "source.pdf",
                i))
            .ToList();
        var queryVector = new EmbeddingVector(CreateRandomVector());

        _repositoryMock
            .Setup(x => x.GetAllChunksAsync())
            .ReturnsAsync(chunks);

        _embeddingGeneratorMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        // Act
        var results = await _sut.SearchAsync("query", maxResults: 3, CancellationToken.None);

        // Assert
        Assert.That(results, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task SearchAsync_GeneratesEmbeddingForQuery()
    {
        // Arrange
        var query = "specific search query";
        var queryVector = new EmbeddingVector(CreateRandomVector());
        var chunk = DocumentChunk.Create("1", "text", CreateRandomVector(), "source.pdf", 1);

        _repositoryMock
            .Setup(x => x.GetAllChunksAsync())
            .ReturnsAsync(new List<DocumentChunk> { chunk });

        _embeddingGeneratorMock
            .Setup(x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        // Act
        await _sut.SearchAsync(query, 10, CancellationToken.None);

        // Assert
        _embeddingGeneratorMock.Verify(
            x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Creates a normalized vector with all elements set to the same value (for testing similarity).
    /// </summary>
    private static float[] CreateNormalizedVector(float value)
    {
        var vector = new float[VectorDimensions];
        Array.Fill(vector, value);
        return vector;
    }

    /// <summary>
    /// Creates a random vector for testing (normalized).
    /// </summary>
    private static float[] CreateRandomVector()
    {
        var random = Random.Shared;
        return Enumerable.Range(0, VectorDimensions)
            .Select(_ => (float)(random.NextDouble() * 2 - 1))
            .ToArray();
    }
}
