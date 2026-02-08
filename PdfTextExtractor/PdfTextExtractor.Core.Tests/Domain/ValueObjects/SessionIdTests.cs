using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.ValueObjects;

[TestFixture]
[TestOf(typeof(SessionId))]
public class SessionIdTests
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
    public void Create_NewSessionId_ReturnsUniqueId()
    {
        // Arrange & Act
        var result1 = SessionId.Create();
        var result2 = SessionId.Create();

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(result1.Value, Is.Not.EqualTo(result2.Value));
    }

    [Test]
    public void FromGuid_ValidGuid_ReturnsSessionId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = SessionId.FromGuid(guid);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(guid));
    }

    [Test]
    public void ImplicitOperator_ValidSessionId_ConvertsToGuid()
    {
        // Arrange
        var sessionId = SessionId.Create();

        // Act
        Guid guid = sessionId;

        // Assert
        Assert.That(guid, Is.EqualTo(sessionId.Value));
    }

    [Test]
    public void Value_ValidSessionId_ReturnsGuid()
    {
        // Arrange
        var sessionId = SessionId.Create();

        // Act
        var value = sessionId.Value;

        // Assert
        Assert.That(value, Is.Not.EqualTo(Guid.Empty));
    }
}
