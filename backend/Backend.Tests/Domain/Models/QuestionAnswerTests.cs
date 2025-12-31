using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.Domain.Models;
using Backend.API.Domain.ValueObjects;
using Backend.Tests.TestInfrastructure;
using NUnit.Framework;

namespace Backend.Tests.Domain.Models;

[TestFixture]
public class QuestionAnswerTests
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
        var answer = "This is a sample answer to the question.";
        var sources = new List<DocumentMetadata>
        {
            new DocumentMetadata("document1.pdf", 1),
            new DocumentMetadata("document2.pdf", 5)
        };

        // Act
        var result = new QuestionAnswer(answer, sources);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Answer, Is.EqualTo(answer));
        Assert.That(result.Sources, Is.EqualTo(sources));
    }

    [Test]
    public void Constructor_EmptySources_IsValid()
    {
        // Arrange
        var answer = "This is a sample answer.";
        var sources = new List<DocumentMetadata>();

        // Act
        var result = new QuestionAnswer(answer, sources);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Sources, Is.Empty);
    }

    [Test]
    public void Constructor_MultipleSources_StoresCorrectly()
    {
        // Arrange
        var answer = "This is a sample answer.";
        var sources = new List<DocumentMetadata>
        {
            new DocumentMetadata("doc1.pdf", 1),
            new DocumentMetadata("doc2.pdf", 2),
            new DocumentMetadata("doc3.pdf", 3)
        };

        // Act
        var result = new QuestionAnswer(answer, sources);

        // Assert
        Assert.That(result.Sources, Has.Count.EqualTo(3));
        Assert.That(result.Sources[0].Source, Is.EqualTo("doc1.pdf"));
        Assert.That(result.Sources[1].Source, Is.EqualTo("doc2.pdf"));
        Assert.That(result.Sources[2].Source, Is.EqualTo("doc3.pdf"));
    }
}
