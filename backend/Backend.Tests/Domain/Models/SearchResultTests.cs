using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.Domain.Models;
using Backend.Tests.TestInfrastructure;
using NUnit.Framework;

namespace Backend.Tests.Domain.Models;

[TestFixture]
public class SearchResultTests
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
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var chunk = _fixture.Create<DocumentChunk>();
        var score = 0.85f;

        // Act
        var result = new SearchResult(chunk, score);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Properties_SetCorrectly()
    {
        // Arrange
        var chunk = _fixture.Create<DocumentChunk>();
        var score = 0.75f;

        // Act
        var sut = new SearchResult(chunk, score);

        // Assert
        Assert.That(sut.Chunk, Is.SameAs(chunk));
        Assert.That(sut.SimilarityScore, Is.EqualTo(score));
    }

    [Test]
    public void Constructor_ScoreZero_IsValid()
    {
        // Arrange
        var chunk = _fixture.Create<DocumentChunk>();
        var score = 0.0f;

        // Act
        var result = new SearchResult(chunk, score);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SimilarityScore, Is.EqualTo(0.0f));
    }

    [Test]
    public void Constructor_ScoreOne_IsValid()
    {
        // Arrange
        var chunk = _fixture.Create<DocumentChunk>();
        var score = 1.0f;

        // Act
        var result = new SearchResult(chunk, score);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SimilarityScore, Is.EqualTo(1.0f));
    }
}
