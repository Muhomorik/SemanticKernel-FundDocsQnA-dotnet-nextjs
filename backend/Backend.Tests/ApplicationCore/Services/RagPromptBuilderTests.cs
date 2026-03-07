using AutoFixture;
using Backend.API.ApplicationCore.Services;
using Backend.API.Domain.Models;
using Backend.Tests.TestInfrastructure;
using NUnit.Framework;

namespace Backend.Tests.ApplicationCore.Services;

[TestFixture]
public class RagPromptBuilderTests
{
    private IFixture _fixture = null!;
    private RagPromptBuilder _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new BackendDomainCustomization());

        _sut = new RagPromptBuilder();
    }

    [Test]
    public void BuildContext_FormatsSearchResultsAsXmlChunks()
    {
        // Arrange
        var chunk1 = DocumentChunk.Create("1", "First content", new float[10], "fund_a.pdf", 1);
        var chunk2 = DocumentChunk.Create("2", "Second content", new float[10], "fund_b.pdf", 3);
        var results = new List<SearchResult>
        {
            new(chunk1, 0.9f),
            new(chunk2, 0.8f)
        };

        // Act
        var context = _sut.BuildContext(results);

        // Assert
        Assert.That(context, Does.Contain("<chunk id=\"1\">"));
        Assert.That(context, Does.Contain("<chunk id=\"2\">"));
        Assert.That(context, Does.Contain("<source>fund_a.pdf</source>"));
        Assert.That(context, Does.Contain("<source>fund_b.pdf</source>"));
        Assert.That(context, Does.Contain("<page>1</page>"));
        Assert.That(context, Does.Contain("<page>3</page>"));
        Assert.That(context, Does.Contain("<content>First content</content>"));
        Assert.That(context, Does.Contain("<content>Second content</content>"));
        Assert.That(context, Does.Contain("</chunk>"));
    }

    [Test]
    public void BuildContext_EmptyResults_ReturnsEmptyString()
    {
        // Act
        var context = _sut.BuildContext(new List<SearchResult>());

        // Assert
        Assert.That(context, Is.Empty);
    }

    [Test]
    public void BuildUserPrompt_WrapsContextAndQuestionInXmlTags()
    {
        // Arrange
        var context = "<chunk id=\"1\"><content>test</content></chunk>";
        var question = "What is the fund?";

        // Act
        var prompt = _sut.BuildUserPrompt(context, question);

        // Assert
        Assert.That(prompt, Does.Contain("<retrieved_context>"));
        Assert.That(prompt, Does.Contain("</retrieved_context>"));
        Assert.That(prompt, Does.Contain(context));
        Assert.That(prompt, Does.Contain("<user_question>"));
        Assert.That(prompt, Does.Contain("</user_question>"));
        Assert.That(prompt, Does.Contain(question));
        Assert.That(prompt, Does.Contain("Answer the user's question using the retrieved context and any available functions."));
    }
}
