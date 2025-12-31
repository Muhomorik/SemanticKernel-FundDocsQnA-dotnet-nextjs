using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.Domain.ValueObjects;
using Backend.Tests.TestInfrastructure;
using NUnit.Framework;

namespace Backend.Tests.Domain.ValueObjects;

[TestFixture]
public class EmbeddingVectorTests
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
    public void Constructor_ValidArray_CreatesInstance()
    {
        // Arrange
        var values = Enumerable.Range(0, 1536)
            .Select(i => (float)i)
            .ToArray();

        // Act
        var result = new EmbeddingVector(values);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Dimensions_ReturnsCorrectLength()
    {
        // Arrange
        var values = Enumerable.Range(0, 1536)
            .Select(i => (float)i)
            .ToArray();
        var sut = new EmbeddingVector(values);

        // Act
        var result = sut.Dimensions;

        // Assert
        Assert.That(result, Is.EqualTo(1536));
    }

    [Test]
    public void Values_ReturnsArrayReference()
    {
        // Arrange
        var values = Enumerable.Range(0, 1536)
            .Select(i => (float)i)
            .ToArray();
        var sut = new EmbeddingVector(values);

        // Act
        var result = sut.Values;

        // Assert
        Assert.That(result, Is.SameAs(values));
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var values = Enumerable.Range(0, 1536)
            .Select(i => (float)i)
            .ToArray();
        var vector1 = new EmbeddingVector(values);
        var vector2 = new EmbeddingVector(values);

        // Act & Assert
        Assert.That(vector1, Is.EqualTo(vector2));
    }
}
