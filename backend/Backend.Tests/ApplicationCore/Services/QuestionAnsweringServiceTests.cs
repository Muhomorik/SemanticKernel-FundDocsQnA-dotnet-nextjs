using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.ApplicationCore.Configuration;
using Backend.API.ApplicationCore.DTOs;
using Backend.API.ApplicationCore.Services;
using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;
using Backend.API.Domain.ValueObjects;
using Backend.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Backend.Tests.ApplicationCore.Services;

[TestFixture]
public class QuestionAnsweringServiceTests
{
    private IFixture _fixture;
    private Mock<ISemanticSearch> _semanticSearchMock;
    private Mock<ILlmProvider> _llmProviderMock;
    private Mock<IUserQuestionSanitizer> _questionSanitizerMock;
    private Mock<ILogger<QuestionAnsweringService>> _loggerMock;
    private QuestionAnsweringService _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new BackendDomainCustomization());

        // Freeze dependencies for reuse across tests
        _semanticSearchMock = _fixture.Freeze<Mock<ISemanticSearch>>();
        _llmProviderMock = _fixture.Freeze<Mock<ILlmProvider>>();
        _questionSanitizerMock = _fixture.Freeze<Mock<IUserQuestionSanitizer>>();
        _loggerMock = _fixture.Freeze<Mock<ILogger<QuestionAnsweringService>>>();

        // Resolve SUT from fixture
        _sut = _fixture.Create<QuestionAnsweringService>();
    }

    #region Happy Path Tests

    [Test]
    public async Task AnswerQuestionAsync_ValidQuestion_ReturnsAnswerWithSources()
    {
        // Arrange
        var request = _fixture.Create<AskQuestionRequest>();
        var searchResults = _fixture.CreateMany<SearchResult>(3).ToList();
        var expectedAnswer = _fixture.Create<string>();

        _questionSanitizerMock
            .Setup(x => x.Sanitize(request.Question))
            .Returns(request.Question);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(request.Question, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnswer);

        // Act
        var result = await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Answer, Is.EqualTo(expectedAnswer));
        Assert.That(result.Sources, Is.Not.Empty);

        _questionSanitizerMock.Verify(x => x.Sanitize(request.Question), Times.Once);
        _semanticSearchMock.Verify(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _llmProviderMock.Verify(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AnswerQuestionAsync_ValidQuestionWithMultipleChunks_BuildsCorrectXmlContext()
    {
        // Arrange
        var request = _fixture.Create<AskQuestionRequest>();
        var searchResults = _fixture.CreateMany<SearchResult>(3).ToList();
        var expectedAnswer = _fixture.Create<string>();
        var capturedUserPrompt = string.Empty;

        _questionSanitizerMock
            .Setup(x => x.Sanitize(request.Question))
            .Returns(request.Question);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, prompt, _) => capturedUserPrompt = prompt)
            .ReturnsAsync(expectedAnswer);

        // Act
        await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        Assert.That(capturedUserPrompt, Does.Contain("<retrieved_context>"));
        Assert.That(capturedUserPrompt, Does.Contain("</retrieved_context>"));
        Assert.That(capturedUserPrompt, Does.Contain("<chunk"));
        Assert.That(capturedUserPrompt, Does.Contain("<source>"));
        Assert.That(capturedUserPrompt, Does.Contain("<page>"));
        Assert.That(capturedUserPrompt, Does.Contain("<content>"));
        Assert.That(capturedUserPrompt, Does.Contain("<user_question>"));
        Assert.That(capturedUserPrompt, Does.Contain(request.Question));
    }

    [Test]
    public async Task AnswerQuestionAsync_QuestionWithDuplicateSources_DeduplicatesSources()
    {
        // Arrange
        var request = _fixture.Create<AskQuestionRequest>();

        // Create search results with duplicate sources (same file and page)
        var chunk1 = DocumentChunk.Create("1", "content1", new float[10], "fund_a.pdf", 1);
        var result1 = new SearchResult(chunk1, 0.9f);

        var chunk2 = DocumentChunk.Create("2", "content2", new float[10], "fund_a.pdf", 1);
        var result2 = new SearchResult(chunk2, 0.8f);

        var chunk3 = DocumentChunk.Create("3", "content3", new float[10], "fund_b.pdf", 2);
        var result3 = new SearchResult(chunk3, 0.7f);

        var searchResults = new List<SearchResult> { result1, result2, result3 };

        _questionSanitizerMock
            .Setup(x => x.Sanitize(request.Question))
            .Returns(request.Question);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        var result = await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Sources, Has.Count.EqualTo(2));
        Assert.That(result.Sources.DistinctBy(s => new { s.File, s.Page }).Count(), Is.EqualTo(2));
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task AnswerQuestionAsync_NoSearchResults_ReturnsNoInformationMessage()
    {
        // Arrange
        var request = _fixture.Create<AskQuestionRequest>();

        _questionSanitizerMock
            .Setup(x => x.Sanitize(request.Question))
            .Returns(request.Question);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>());

        // Act
        var result = await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Answer, Is.EqualTo("I don't have enough information to answer this question."));
        Assert.That(result.Sources, Is.Empty);
        _llmProviderMock.Verify(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AnswerQuestionAsync_QuestionRequiringSanitization_SanitizesAndProcesses()
    {
        // Arrange
        var originalQuestion = "What is the fund?\x00\x01\x02";  // Control characters
        var sanitizedQuestion = "What is the fund?";
        var request = _fixture.Build<AskQuestionRequest>()
            .With(x => x.Question, originalQuestion)
            .Create();
        var searchResults = _fixture.CreateMany<SearchResult>(1).ToList();

        _questionSanitizerMock
            .Setup(x => x.Sanitize(originalQuestion))
            .Returns(sanitizedQuestion);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(sanitizedQuestion, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        _questionSanitizerMock.Verify(x => x.Sanitize(originalQuestion), Times.Once);
        _semanticSearchMock.Verify(x => x.SearchAsync(sanitizedQuestion, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AnswerQuestionAsync_LargeQuestionString_HandlesCorrectly()
    {
        // Arrange
        var largeQuestion = string.Concat(Enumerable.Repeat("What is the fund? ", 100)); // ~1800 characters
        var request = _fixture.Build<AskQuestionRequest>()
            .With(x => x.Question, largeQuestion)
            .Create();
        var searchResults = _fixture.CreateMany<SearchResult>(2).ToList();
        var expectedAnswer = _fixture.Create<string>();

        _questionSanitizerMock
            .Setup(x => x.Sanitize(largeQuestion))
            .Returns(largeQuestion);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnswer);

        // Act
        var result = await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Answer, Is.EqualTo(expectedAnswer));
        Assert.That(result.Sources, Is.Not.Empty);
    }

    [Test]
    public async Task AnswerQuestionAsync_SanitizationRemovesSignificantContent_LogsWarning()
    {
        // Arrange
        var originalQuestion = "What is the fund?" + string.Concat(Enumerable.Repeat("\x00", 100)); // 90% control chars
        var sanitizedQuestion = "What is the fund?";
        var request = _fixture.Build<AskQuestionRequest>()
            .With(x => x.Question, originalQuestion)
            .Create();
        var searchResults = _fixture.CreateMany<SearchResult>(1).ToList();

        _questionSanitizerMock
            .Setup(x => x.Sanitize(originalQuestion))
            .Returns(sanitizedQuestion);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sanitization removed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region Service Integration

    [Test]
    public async Task AnswerQuestionAsync_CallsServicesInCorrectOrder()
    {
        // Arrange
        var request = _fixture.Create<AskQuestionRequest>();
        var searchResults = _fixture.CreateMany<SearchResult>(1).ToList();
        var callOrder = new List<string>();

        _questionSanitizerMock
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Callback(() => callOrder.Add("sanitize"))
            .Returns(request.Question);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("search"))
            .ReturnsAsync(searchResults);

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("llm"))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        Assert.That(callOrder, Is.EqualTo(new[] { "sanitize", "search", "llm" }));
    }

    [Test]
    public async Task AnswerQuestionAsync_PassesCorrectMaxResultsToSearch()
    {
        // Arrange
        var request = _fixture.Create<AskQuestionRequest>();
        var searchResults = _fixture.CreateMany<SearchResult>(1).ToList();
        var applicationOptions = _fixture.Freeze<ApplicationOptions>();
        var capturedMaxResults = 0;

        _questionSanitizerMock
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns(request.Question);

        _semanticSearchMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, CancellationToken>((_, maxResults, _) => capturedMaxResults = maxResults)
            .ReturnsAsync(searchResults);

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        await _sut.AnswerQuestionAsync(request, CancellationToken.None);

        // Assert
        Assert.That(capturedMaxResults, Is.EqualTo(applicationOptions.MaxSearchResults));
    }

    #endregion
}
