using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.ValueObjects;

[TestFixture]
[TestOf(typeof(CorrelationId))]
public class CorrelationIdTests
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
    public void Create_NewCorrelationId_ReturnsUniqueId()
    {
        // Arrange & Act
        var result1 = CorrelationId.Create();
        var result2 = CorrelationId.Create();

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(result1.Value, Is.Not.EqualTo(result2.Value));
    }

    [Test]
    public void FromGuid_ValidGuid_ReturnsCorrelationId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = CorrelationId.FromGuid(guid);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(guid));
    }

    [Test]
    public void ImplicitOperator_ValidCorrelationId_ConvertsToGuid()
    {
        // Arrange
        var correlationId = CorrelationId.Create();

        // Act
        Guid guid = correlationId;

        // Assert
        Assert.That(guid, Is.EqualTo(correlationId.Value));
    }

    [Test]
    public void Value_ValidCorrelationId_ReturnsGuid()
    {
        // Arrange
        var correlationId = CorrelationId.Create();

        // Act
        var value = correlationId.Value;

        // Assert
        Assert.That(value, Is.Not.EqualTo(Guid.Empty));
    }
}
