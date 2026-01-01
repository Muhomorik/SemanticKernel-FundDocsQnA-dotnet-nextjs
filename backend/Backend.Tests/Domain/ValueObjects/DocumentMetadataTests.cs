using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.Domain.ValueObjects;
using Backend.Tests.TestInfrastructure;
using NUnit.Framework;

namespace Backend.Tests.Domain.ValueObjects;

[TestFixture]
public class DocumentMetadataTests
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
        var source = "test-document.pdf";
        var page = 42;

        // Act
        var result = new DocumentMetadata(source, page);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Properties_ReturnCorrectValues()
    {
        // Arrange
        var source = "test-document.pdf";
        var page = 42;
        var sut = new DocumentMetadata(source, page);

        // Act & Assert
        Assert.That(sut.Source, Is.EqualTo(source));
        Assert.That(sut.Page, Is.EqualTo(page));
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var source = "test-document.pdf";
        var page = 42;
        var metadata1 = new DocumentMetadata(source, page);
        var metadata2 = new DocumentMetadata(source, page);

        // Act & Assert
        Assert.That(metadata1, Is.EqualTo(metadata2));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var metadata1 = new DocumentMetadata("document1.pdf", 1);
        var metadata2 = new DocumentMetadata("document2.pdf", 2);

        // Act & Assert
        Assert.That(metadata1, Is.Not.EqualTo(metadata2));
    }
}
