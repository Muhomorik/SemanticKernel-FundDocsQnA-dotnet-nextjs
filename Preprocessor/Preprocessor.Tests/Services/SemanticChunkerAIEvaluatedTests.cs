using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Preprocessor.Services;
using Preprocessor.Tests.TestHelpers;
using System.Text.Json;

namespace Preprocessor.Tests.Services;

/// <summary>
/// Integration tests for SemanticChunker with AI-based quality evaluation.
/// Uses OpenAI to evaluate chunk coherence, completeness, and RAG usefulness.
///
/// <para><b>!!! AI AGENT - DO NOT RUN TESTS AUTOMATICALLY !!!</b></para>
/// <para>
/// These tests make real API calls to OpenAI and incur costs.
/// The user MUST explicitly request running these tests.
/// </para>
///
/// <para><b>How to run:</b></para>
/// <code>
/// cd Preprocessor/Preprocessor.Tests
/// dotnet user-secrets set "BackendOptions:OpenAIApiKey" "sk-..."
/// dotnet test --filter "FullyQualifiedName~SemanticChunkerAIEvaluatedTests" --logger "console;verbosity=detailed"
/// </code>
/// </summary>
[TestFixture]
[TestOf(typeof(SemanticChunker))]
[Category("Integration")]
[Explicit("Requires OpenAI API key in user secrets (BackendOptions:OpenAIApiKey)")]
public class SemanticChunkerAIEvaluatedTests
{
    private const string EvaluationModel = "gpt-4o-mini";

    private string _apiKey = null!;
    private Kernel _kernel = null!;
    private IChatCompletionService _chatService = null!;
    private SemanticChunker _sut = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<SemanticChunkerAIEvaluatedTests>()
            .Build();

        _apiKey = configuration["BackendOptions:OpenAIApiKey"] ?? string.Empty;

        var keyLoaded = !string.IsNullOrWhiteSpace(_apiKey);
        TestContext.Out.WriteLine($"API key loaded from user secrets: {keyLoaded}");

        if (!keyLoaded)
        {
            Assert.Ignore(
                "OpenAI API key not found in user secrets.\n" +
                "Set it using: dotnet user-secrets set \"BackendOptions:OpenAIApiKey\" \"sk-...\"");
        }

        _kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(EvaluationModel, _apiKey)
            .Build();

        _chatService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    [SetUp]
    public void SetUp()
    {
        _sut = new SemanticChunker(maxChunkSize: 800, overlapPercentage: 0.15);
    }

    #region Page 1 Tests - Fund Header & Risk Indicator

    [Test]
    public async Task Chunk_Page1FundHeader_AIEvaluatesHighCoherence()
    {
        // Arrange
        Assert.That(TestFiles.Exists(TestFiles.PdfExamplePage1Text), Is.True,
            $"Test file not found: {TestFiles.PdfExamplePage1Text}");
        var text = await File.ReadAllTextAsync(TestFiles.PdfExamplePage1Text);

        // Act
        var chunks = _sut.Chunk(text).ToList();
        var evaluation = await EvaluateChunksAsync(chunks);

        // Assert
        Assert.That(chunks, Is.Not.Empty);
        Assert.That(evaluation.Coherence, Is.GreaterThanOrEqualTo(7),
            $"Coherence score too low. Issues: {string.Join(", ", evaluation.Issues)}");
        LogEvaluation("Page 1 - Fund Header", chunks, evaluation);
    }

    #endregion

    #region Page 2 Tests - Performance Scenarios Table

    [Test]
    public async Task Chunk_Page2PerformanceScenarios_AIEvaluatesRAGUsefulness()
    {
        // Arrange
        Assert.That(TestFiles.Exists(TestFiles.PdfExamplePage2Text), Is.True,
            $"Test file not found: {TestFiles.PdfExamplePage2Text}");
        var text = await File.ReadAllTextAsync(TestFiles.PdfExamplePage2Text);

        // Act
        var chunks = _sut.Chunk(text).ToList();
        var evaluation = await EvaluateChunksAsync(chunks,
            "Can this answer: What happens to my investment in a stress scenario?");

        // Assert
        Assert.That(evaluation.Usefulness, Is.GreaterThanOrEqualTo(7),
            $"Usefulness score too low. Issues: {string.Join(", ", evaluation.Issues)}");
        LogEvaluation("Page 2 - Performance Scenarios", chunks, evaluation);
    }

    #endregion

    #region Page 3 Tests - Cost Breakdown

    [Test]
    public async Task Chunk_Page3CostBreakdown_AIEvaluatesFeeInfoComplete()
    {
        // Arrange
        Assert.That(TestFiles.Exists(TestFiles.PdfExamplePage3Text), Is.True,
            $"Test file not found: {TestFiles.PdfExamplePage3Text}");
        var text = await File.ReadAllTextAsync(TestFiles.PdfExamplePage3Text);

        // Act
        var chunks = _sut.Chunk(text).ToList();
        var evaluation = await EvaluateChunksAsync(chunks,
            "Can this answer: What are the management fees for this fund?");

        // Assert
        Assert.That(evaluation.Usefulness, Is.GreaterThanOrEqualTo(7),
            $"Usefulness score too low. Issues: {string.Join(", ", evaluation.Issues)}");
        Assert.That(string.Join(" ", chunks), Does.Contain("1,52%"),
            "Fee percentage 1,52% should be preserved in chunks");
        LogEvaluation("Page 3 - Cost Breakdown", chunks, evaluation);
    }

    #endregion

    #region Multi-Page Tests

    [Test]
    public async Task Chunk_AllPages_AIEvaluatesOverallQuality()
    {
        // Arrange
        Assert.That(TestFiles.Exists(TestFiles.PdfExamplePage1Text), Is.True);
        Assert.That(TestFiles.Exists(TestFiles.PdfExamplePage2Text), Is.True);
        Assert.That(TestFiles.Exists(TestFiles.PdfExamplePage3Text), Is.True);

        var allText = string.Join("\n\n",
            await File.ReadAllTextAsync(TestFiles.PdfExamplePage1Text),
            await File.ReadAllTextAsync(TestFiles.PdfExamplePage2Text),
            await File.ReadAllTextAsync(TestFiles.PdfExamplePage3Text));

        // Act
        var chunks = _sut.Chunk(allText).ToList();
        var evaluation = await EvaluateChunksAsync(chunks);

        // Assert
        Assert.That(chunks, Has.Count.GreaterThan(1),
            "Combined document should produce multiple chunks");
        Assert.That(evaluation.Coherence, Is.GreaterThanOrEqualTo(6),
            $"Coherence score too low. Issues: {string.Join(", ", evaluation.Issues)}");
        Assert.That(evaluation.Completeness, Is.GreaterThanOrEqualTo(6),
            $"Completeness score too low. Issues: {string.Join(", ", evaluation.Issues)}");
        LogEvaluation("All Pages Combined", chunks, evaluation);
    }

    #endregion

    #region Helper Methods

    private async Task<ChunkEvaluation> EvaluateChunksAsync(
        List<string> chunks,
        string? ragQuestion = null)
    {
        var chunksText = string.Join("\n\n---CHUNK BOUNDARY---\n\n", chunks);
        var questionContext = ragQuestion != null
            ? $"\n\nRAG Question to answer: {ragQuestion}"
            : "";

        const string jsonFormat = """{"coherence":N,"completeness":N,"usefulness":N,"issues":["..."]}""";

        var prompt = $"""
            Evaluate these text chunks for a RAG system about investment funds.

            Chunks:
            ---
            {chunksText}
            ---
            {questionContext}

            Score 1-10:
            1. Coherence: Is each chunk about one topic? No mixed unrelated content?
            2. Completeness: Are sentences complete? No cut-off text?
            3. Usefulness: Would these chunks help answer questions about the fund?

            Respond ONLY with JSON (no markdown): {jsonFormat}
            """;

        TestContext.Out.WriteLine("--- EVALUATION PROMPT ---");
        TestContext.Out.WriteLine(prompt);
        TestContext.Out.WriteLine("--- EVALUATION PROMPT END ---");
        TestContext.Out.WriteLine();

        var result = await _chatService.GetChatMessageContentAsync(prompt);

        TestContext.Out.WriteLine("--- AI RESPONSE ---");
        TestContext.Out.WriteLine(result.Content);
        TestContext.Out.WriteLine("--- AI RESPONSE END ---");
        TestContext.Out.WriteLine();

        var jsonContent = result.Content!.Trim();

        // Handle potential markdown code blocks in response
        if (jsonContent.StartsWith("```"))
        {
            var lines = jsonContent.Split('\n');
            jsonContent = string.Join("\n", lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
        }

        return JsonSerializer.Deserialize<ChunkEvaluation>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    private void LogEvaluation(string testName, List<string> chunks, ChunkEvaluation eval)
    {
        TestContext.Out.WriteLine($"=== {testName} Evaluation ===");
        TestContext.Out.WriteLine($"Chunks: {chunks.Count}");
        TestContext.Out.WriteLine($"Total characters: {chunks.Sum(c => c.Length)}");
        TestContext.Out.WriteLine();

        for (var i = 0; i < chunks.Count; i++)
        {
            TestContext.Out.WriteLine($"--- Chunk {i + 1} ({chunks[i].Length} chars) ---");
            TestContext.Out.WriteLine(chunks[i].Length > 200
                ? chunks[i][..200] + "..."
                : chunks[i]);
            TestContext.Out.WriteLine();
        }

        TestContext.Out.WriteLine($"Coherence: {eval.Coherence}/10");
        TestContext.Out.WriteLine($"Completeness: {eval.Completeness}/10");
        TestContext.Out.WriteLine($"Usefulness: {eval.Usefulness}/10");

        if (eval.Issues.Count > 0)
        {
            TestContext.Out.WriteLine($"Issues: {string.Join(", ", eval.Issues)}");
        }

        TestContext.Out.WriteLine();
    }

    private record ChunkEvaluation(
        int Coherence,
        int Completeness,
        int Usefulness,
        List<string> Issues);

    #endregion
}
