using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.Domain.Services;
using Backend.API.Domain.ValueObjects;
using Backend.Tests.TestInfrastructure;
using NUnit.Framework;

namespace Backend.Tests.Domain.Services;

[TestFixture]
public class CosineSimilarityCalculatorTests
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
    public void Calculate_IdenticalVectors_ReturnsOne()
    {
        // Arrange
        var values = new[] { 1.0f, 2.0f, 3.0f, 4.0f };
        var vector = new EmbeddingVector(values);

        // Act
        var result = CosineSimilarityCalculator.Calculate(vector, vector);

        // Assert
        Assert.That(result, Is.EqualTo(1.0f).Within(0.0001f));
    }

    [Test]
    public void Calculate_OrthogonalVectors_ReturnsZero()
    {
        // Arrange
        var vector1 = new EmbeddingVector(new[] { 1.0f, 0.0f, 0.0f });
        var vector2 = new EmbeddingVector(new[] { 0.0f, 1.0f, 0.0f });

        // Act
        var result = CosineSimilarityCalculator.Calculate(vector1, vector2);

        // Assert
        Assert.That(result, Is.EqualTo(0.0f).Within(0.0001f));
    }

    [Test]
    public void Calculate_OppositeVectors_ReturnsNegativeOne()
    {
        // Arrange
        var vector1 = new EmbeddingVector(new[] { 1.0f, 2.0f, 3.0f });
        var vector2 = new EmbeddingVector(new[] { -1.0f, -2.0f, -3.0f });

        // Act
        var result = CosineSimilarityCalculator.Calculate(vector1, vector2);

        // Assert
        Assert.That(result, Is.EqualTo(-1.0f).Within(0.0001f));
    }

    [Test]
    public void Calculate_ZeroMagnitudeVector_ReturnsZero()
    {
        // Arrange
        var vector1 = new EmbeddingVector(new[] { 0.0f, 0.0f, 0.0f });
        var vector2 = new EmbeddingVector(new[] { 1.0f, 2.0f, 3.0f });

        // Act
        var result = CosineSimilarityCalculator.Calculate(vector1, vector2);

        // Assert
        Assert.That(result, Is.EqualTo(0.0f));
    }

    [Test]
    public void Calculate_IsSymmetric()
    {
        // Arrange
        var vector1 = new EmbeddingVector(new[] { 1.0f, 2.0f, 3.0f });
        var vector2 = new EmbeddingVector(new[] { 4.0f, 5.0f, 6.0f });

        // Act
        var result1 = CosineSimilarityCalculator.Calculate(vector1, vector2);
        var result2 = CosineSimilarityCalculator.Calculate(vector2, vector1);

        // Assert
        Assert.That(result1, Is.EqualTo(result2).Within(0.0001f));
    }

    [Test]
    public void Calculate_ResultInValidRange()
    {
        // Arrange
        var values1 = Enumerable.Range(0, 100).Select(i => (float)Random.Shared.NextDouble()).ToArray();
        var values2 = Enumerable.Range(0, 100).Select(i => (float)Random.Shared.NextDouble()).ToArray();
        var vector1 = new EmbeddingVector(values1);
        var vector2 = new EmbeddingVector(values2);

        // Act
        var result = CosineSimilarityCalculator.Calculate(vector1, vector2);

        // Assert
        Assert.That(result, Is.GreaterThanOrEqualTo(-1.0f));
        Assert.That(result, Is.LessThanOrEqualTo(1.0f));
    }
}
