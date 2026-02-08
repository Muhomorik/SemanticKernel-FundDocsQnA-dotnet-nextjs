using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Preprocessor.Tests.TestHelpers;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Preprocessor.Tests.Services;

/// <summary>
/// AI-evaluated tests for example queries to verify answerability against PRIIP/KID documents.
/// Each test category is evaluated separately with appropriate expectations.
///
/// <para><b>!!! AI AGENT - DO NOT RUN TESTS AUTOMATICALLY !!!</b></para>
/// <para>
/// These tests make real API calls to OpenAI and incur costs.
/// The user MUST explicitly request running these tests.
/// </para>
///
/// <para><b>Purpose:</b></para>
/// <para>
/// This is a DISCOVERY TOOL to identify invalid/problematic queries. The workflow is:
/// 1. Run tests to evaluate queries against the AI prompt
/// 2. Review test output to identify queries marked as info_missing, context_dependent, etc.
/// 3. MANUALLY update source files based on findings:
///    - @frontend/lib/example-queries-data.ts (production UI - shown to users)
///    - @Preprocessor/Preprocessor.Tests/TestData/example_queries.json (test data mirror)
/// 4. Re-run tests to verify fixes
/// </para>
/// <para>
/// DO NOT automatically modify source files - only update the evaluation prompt in this file.
/// </para>
///
/// <para><b>RAG System Context:</b></para>
/// <para>
/// The RAG system has NO mechanism to establish fund-specific context:
/// - No fund selection UI (users cannot pick a specific fund)
/// - No conversation history (each query is standalone)
/// - RAG retrieval alone does NOT provide "the fund" context
/// Therefore, queries using "this fund", "the fund's", or implying a specific fund
/// are context_dependent and should be flagged for rephrasing or removal.
/// </para>
///
/// <para><b>How to run:</b></para>
/// <code>
/// cd Preprocessor/Preprocessor.Tests
/// dotnet user-secrets set "OpenAIApiKey" "sk-..."
///
/// # Run all AI-evaluated tests
/// dotnet test --filter "FullyQualifiedName~ExampleQueriesAIEvaluatedTests" --logger "console;verbosity=detailed"
///
/// # Run specific category
/// dotnet test --filter "Name~QuickStart" --logger "console;verbosity=detailed"
/// dotnet test --filter "Name~SingleFundQuestions" --logger "console;verbosity=detailed"
/// </code>
/// </summary>
[TestFixture]
[Category("Integration")]
[Explicit("Requires OpenAI API key in user secrets (OpenAIApiKey)")]
public class ExampleQueriesAIEvaluatedTests
{
    private const string EvaluationModel = "gpt-4o-mini";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string _apiKey = null!;
    private Kernel _kernel = null!;
    private IChatCompletionService _chatService = null!;
    private string _documentContent = null!;
    private QueryData _queryData = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ExampleQueriesAIEvaluatedTests>()
            .Build();

        _apiKey = configuration["OpenAIApiKey"] ?? string.Empty;

        var keyLoaded = !string.IsNullOrWhiteSpace(_apiKey);
        TestContext.Out.WriteLine($"API key loaded from user secrets: {keyLoaded}");

        if (!keyLoaded)
        {
            Assert.Ignore(
                "OpenAI API key not found in user secrets.\n" +
                "Set it using: dotnet user-secrets set \"OpenAIApiKey\" \"sk-...\"");
        }

        _kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(EvaluationModel, _apiKey)
            .Build();

        _chatService = _kernel.GetRequiredService<IChatCompletionService>();

        // Load document content
        Assert.That(TestFiles.Exists(TestFiles.PdfExampleText), Is.True,
            $"Test file not found: {TestFiles.PdfExampleText}");
        _documentContent = await File.ReadAllTextAsync(TestFiles.PdfExampleText);

        // Load query data
        Assert.That(TestFiles.Exists(TestFiles.ExampleQueriesJson), Is.True,
            $"Test file not found: {TestFiles.ExampleQueriesJson}");
        var queriesJson = await File.ReadAllTextAsync(TestFiles.ExampleQueriesJson);
        _queryData = JsonSerializer.Deserialize<QueryData>(queriesJson, JsonOptions)!;
    }

    #region Quick Start Tests

    [Test]
    public async Task QuickStart_GettingStarted_QueriesShouldBeAnswerable()
    {
        // These are general queries that work with multi-doc RAG
        // Expected: multi_doc_answerable or single_doc_answerable
        var queries = GetQueriesForCategory("Quick Start", "Getting Started");
        var results = await EvaluateQueriesAsync(queries, "Getting Started");

        LogResults("Quick Start - Getting Started", results);

        // Soft assertion - most should be production valid
        var productionValid = results.Count(r => r.Eval.ProductionValid);
        TestContext.Out.WriteLine($"\nProduction valid: {productionValid}/{results.Count}");
    }

    #endregion

    #region Comparison Questions Tests

    [Test]
    public async Task ComparisonQuestions_DirectComparisons_ShouldBeMultiDocAnswerable()
    {
        // These require multiple documents - expected: multi_doc_answerable
        var queries = GetQueriesForCategory("Comparison Questions", "Direct Comparisons");
        var results = await EvaluateQueriesAsync(queries, "Direct Comparisons");

        LogResults("Comparison Questions - Direct Comparisons", results);

        var multiDoc = results.Count(r => r.Eval.Category == "multi_doc_answerable");
        TestContext.Out.WriteLine($"\nMulti-doc answerable: {multiDoc}/{results.Count}");

        // All comparison queries should be multi-doc answerable
        Assert.That(multiDoc, Is.EqualTo(results.Count),
            "All direct comparison queries should be multi_doc_answerable");
    }

    [Test]
    public async Task ComparisonQuestions_BestWorstAnalysis_ShouldBeMultiDocAnswerable()
    {
        var queries = GetQueriesForCategory("Comparison Questions", "Best/Worst Analysis");
        var results = await EvaluateQueriesAsync(queries, "Best/Worst Analysis");

        LogResults("Comparison Questions - Best/Worst Analysis", results);

        var multiDoc = results.Count(r => r.Eval.Category == "multi_doc_answerable");
        TestContext.Out.WriteLine($"\nMulti-doc answerable: {multiDoc}/{results.Count}");

        Assert.That(multiDoc, Is.EqualTo(results.Count),
            "All best/worst analysis queries should be multi_doc_answerable");
    }

    [Test]
    public async Task ComparisonQuestions_Thematic_ShouldBeMultiDocAnswerable()
    {
        var queries = GetQueriesForCategory("Comparison Questions", "Thematic");
        var results = await EvaluateQueriesAsync(queries, "Thematic");

        LogResults("Comparison Questions - Thematic", results);

        var multiDoc = results.Count(r => r.Eval.Category == "multi_doc_answerable");
        TestContext.Out.WriteLine($"\nMulti-doc answerable: {multiDoc}/{results.Count}");
    }

    #endregion

    #region Specific Funds Tests

    [Test]
    public async Task SpecificFunds_SEBAsienfondExJapan_QueriesShouldBeAnswerableFromTestDoc()
    {
        // This is the fund we have a test document for
        // Expected: single_doc_answerable
        var queries = GetQueriesForCategory("Specific Funds", "SEB Asienfond ex Japan");
        var results = await EvaluateQueriesAsync(queries, "SEB Asienfond ex Japan");

        LogResults("Specific Funds - SEB Asienfond ex Japan", results);

        var answerable = results.Count(r =>
            r.Eval.Category is "single_doc_answerable" or "multi_doc_answerable");
        TestContext.Out.WriteLine($"\nAnswerable from test doc: {answerable}/{results.Count}");

        // At least some should be answerable since we have this fund's document
        Assert.That(answerable, Is.GreaterThan(0),
            "At least one query should be answerable from SEB Asienfond ex Japan document");
    }

    [Test]
    [Description("These queries are for funds NOT in test data - expected to show info_missing")]
    public async Task SpecificFunds_OtherFunds_ExpectedInfoMissing()
    {
        // These funds don't have test documents - we just verify queries are well-formed
        var fundCategories = new[]
        {
            "SEB Korträntefond SEK",
            "SEB European Defence & Security",
            "SEB Världenfond",
            "SEB Global High Yield",
            "SEB USA Indexnära"
        };

        var allResults = new List<(string Category, string Query, QueryEvaluation Eval)>();

        foreach (var fundCategory in fundCategories)
        {
            var queries = GetQueriesForCategory("Specific Funds", fundCategory);
            if (queries.Count == 0) continue;

            var results = await EvaluateQueriesAsync(queries, fundCategory);
            allResults.AddRange(results.Select(r => (fundCategory, r.Query, r.Eval)));
        }

        TestContext.Out.WriteLine("\n=== Specific Funds (Other) - Expected info_missing ===\n");
        foreach (var (category, query, eval) in allResults)
        {
            var status = eval.Category == "info_missing" ? "✅ Expected" : "⚠️ Unexpected";
            TestContext.Out.WriteLine($"[{status}] [{eval.Category}] {query}");
        }

        // Info: These are expected to be info_missing since we don't have their documents
        var infoMissing = allResults.Count(r => r.Eval.Category == "info_missing");
        TestContext.Out.WriteLine($"\nInfo missing (expected): {infoMissing}/{allResults.Count}");
    }

    #endregion

    #region Single Fund Questions Tests

    [Test]
    public async Task SingleFundQuestions_AboutTheFund_ShouldBeSingleDocAnswerable()
    {
        // Generic "this fund" questions - work with RAG context
        // Expected: single_doc_answerable (RAG provides context)
        var queries = GetQueriesForCategory("Single Fund Questions", "About the Fund");
        var results = await EvaluateQueriesAsync(queries, "About the Fund");

        LogResults("Single Fund Questions - About the Fund", results);

        var answerable = results.Count(r =>
            r.Eval.Category == "single_doc_answerable");
        TestContext.Out.WriteLine($"\nSingle-doc answerable: {answerable}/{results.Count}");
    }

    [Test]
    public async Task SingleFundQuestions_RiskAndReturns_ShouldBeSingleDocAnswerable()
    {
        var queries = GetQueriesForCategory("Single Fund Questions", "Risk & Returns");
        var results = await EvaluateQueriesAsync(queries, "Risk & Returns");

        LogResults("Single Fund Questions - Risk & Returns", results);

        var answerable = results.Count(r =>
            r.Eval.Category == "single_doc_answerable");
        TestContext.Out.WriteLine($"\nSingle-doc answerable: {answerable}/{results.Count}");
    }

    [Test]
    public async Task SingleFundQuestions_Costs_ShouldBeSingleDocAnswerable()
    {
        var queries = GetQueriesForCategory("Single Fund Questions", "Costs");
        var results = await EvaluateQueriesAsync(queries, "Costs");

        LogResults("Single Fund Questions - Costs", results);

        var answerable = results.Count(r =>
            r.Eval.Category == "single_doc_answerable");
        TestContext.Out.WriteLine($"\nSingle-doc answerable: {answerable}/{results.Count}");
    }

    [Test]
    public async Task SingleFundQuestions_Investing_ShouldBeSingleDocAnswerable()
    {
        var queries = GetQueriesForCategory("Single Fund Questions", "Investing");
        var results = await EvaluateQueriesAsync(queries, "Investing");

        LogResults("Single Fund Questions - Investing", results);

        var answerable = results.Count(r =>
            r.Eval.Category == "single_doc_answerable");
        TestContext.Out.WriteLine($"\nSingle-doc answerable: {answerable}/{results.Count}");
    }

    [Test]
    public async Task SingleFundQuestions_PracticalInfo_ShouldBeSingleDocAnswerable()
    {
        var queries = GetQueriesForCategory("Single Fund Questions", "Practical Info");
        var results = await EvaluateQueriesAsync(queries, "Practical Info");

        LogResults("Single Fund Questions - Practical Info", results);

        var answerable = results.Count(r =>
            r.Eval.Category == "single_doc_answerable");
        TestContext.Out.WriteLine($"\nSingle-doc answerable: {answerable}/{results.Count}");
    }

    #endregion

    #region Full Report (Optional)

    [Test]
    [Description("Generates full report across all categories - for comprehensive analysis")]
    public async Task GenerateFullReport()
    {
        var allEvaluations = new List<(string Group, string Category, string Query, QueryEvaluation Eval)>();

        foreach (var group in _queryData.Groups)
        {
            foreach (var category in group.Categories)
            {
                foreach (var query in category.Queries)
                {
                    TestContext.Out.WriteLine($"Evaluating: {query}");
                    var evaluation = await EvaluateQueryAsync(query, category.Title);
                    allEvaluations.Add((group.Title, category.Title, query, evaluation));
                    await Task.Delay(500);
                }
            }
        }

        var report = GenerateReport(allEvaluations);
        await File.WriteAllTextAsync(TestFiles.QueryEvaluationReport, report);
        TestContext.Out.WriteLine($"\nReport written to: {TestFiles.QueryEvaluationReport}");

        var summary = GenerateSummary(allEvaluations);
        TestContext.Out.WriteLine(summary);
    }

    #endregion

    #region Helper Methods

    private List<string> GetQueriesForCategory(string groupTitle, string categoryTitle)
    {
        var group = _queryData.Groups.FirstOrDefault(g => g.Title == groupTitle);
        var category = group?.Categories.FirstOrDefault(c => c.Title == categoryTitle);
        return category?.Queries ?? [];
    }

    private async Task<List<(string Query, QueryEvaluation Eval)>> EvaluateQueriesAsync(
        List<string> queries, string categoryContext)
    {
        var results = new List<(string Query, QueryEvaluation Eval)>();

        foreach (var query in queries)
        {
            TestContext.Out.WriteLine($"Evaluating: {query}");
            var evaluation = await EvaluateQueryAsync(query, categoryContext);
            results.Add((query, evaluation));
            await Task.Delay(500); // Rate limiting
        }

        return results;
    }

    private async Task<QueryEvaluation> EvaluateQueryAsync(string query, string categoryContext)
    {
        var docSnippet = _documentContent[..Math.Min(_documentContent.Length, 4000)];
        var prompt = $$"""
                       You are evaluating queries for a RAG system about investment fund documents (PRIIP/KID factsheets).
                       The system has multiple fund documents in its database, but users cannot select a specific fund in the UI.

                       Document content (example PRIIP/KID for SEB Asienfond ex Japan):
                       ---
                       {{docSnippet}}
                       ---

                       Query to evaluate: "{{query}}"
                       Category context: "{{categoryContext}}"

                       Categorize this query:

                       1. "single_doc_answerable" - Can be answered from ONE document using FACTS explicitly stated
                          - Examples: "What is the risk level?", "What are the costs?", "What does this fund invest in?"
                          - Geographic focus IS typically stated (Asian markets, Swedish stocks, emerging markets, etc.)
                          - Sector/industry focus IS typically stated (defense, technology, bonds, equities, etc.)

                       2. "multi_doc_answerable" - Requires FACTUAL comparison or filtering across multiple documents
                          - For objective, measurable attributes: risk levels (1-7), costs (%), returns (%), dates
                          - For filtering by stated characteristics: sector, geography, asset type
                          - "Compare all X funds" queries ARE multi_doc_answerable (comparing factual attributes)
                          - Examples: "Which fund has the lowest risk?", "Compare all bond funds", "Compare all Swedish equity funds"
                          - NOT for subjective judgments or recommendations

                       3. "context_dependent" - Assumes a fund is already selected ("this fund", "the fund's...") without naming it
                          - These need rephrasing to work without fund selection

                       4. "info_missing" - Information NOT present in PRIIP/KID documents, including:
                          - Subjective recommendations: "best for beginners", "suitable for conservative investors"
                          - Financial advice: "which fund should I choose", "what's best for my situation"
                          - Investor profiling: Questions requiring judgment about investor types/experience levels
                          - Data not in documents: fund manager names, detailed portfolio holdings, historical NAV
                          - IMPORTANT: "Best for X type of person" is ALWAYS info_missing - documents don't categorize by investor experience

                       Respond ONLY with JSON (no markdown):
                       {"category":"...","testable":true/false,"productionValid":true/false,"suggestedRephrase":"..." or null,"reason":"..."}
                       """;

        var result = await _chatService.GetChatMessageContentAsync(prompt);
        var jsonContent = result.Content!.Trim();

        if (jsonContent.StartsWith("```"))
        {
            var lines = jsonContent.Split('\n');
            jsonContent = string.Join("\n", lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
        }

        return JsonSerializer.Deserialize<QueryEvaluation>(jsonContent, JsonOptions)!;
    }

    private static void LogResults(string title, List<(string Query, QueryEvaluation Eval)> results)
    {
        TestContext.Out.WriteLine($"\n=== {title} ===\n");

        foreach (var (query, eval) in results)
        {
            var icon = eval.ProductionValid ? "✅" : "❌";
            TestContext.Out.WriteLine($"{icon} [{eval.Category}] {query}");
            if (!string.IsNullOrEmpty(eval.SuggestedRephrase))
            {
                TestContext.Out.WriteLine($"   → Suggested: {eval.SuggestedRephrase}");
            }
        }
    }

    private static string GenerateReport(
        List<(string Group, string Category, string Query, QueryEvaluation Eval)> evaluations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Query Answerability Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine("Document: SEB Asienfond ex Japan (pdf_example.txt)");
        sb.AppendLine();

        // Summary
        sb.AppendLine("## Summary");
        sb.AppendLine();
        var total = evaluations.Count;
        var singleDoc = evaluations.Count(e => e.Eval.Category == "single_doc_answerable");
        var multiDoc = evaluations.Count(e => e.Eval.Category == "multi_doc_answerable");
        var contextDep = evaluations.Count(e => e.Eval.Category == "context_dependent");
        var infoMissing = evaluations.Count(e => e.Eval.Category == "info_missing");

        sb.AppendLine($"- **Total queries:** {total}");
        sb.AppendLine($"- **Single-doc answerable:** {singleDoc} ({100.0 * singleDoc / total:F0}%) ✅");
        sb.AppendLine($"- **Multi-doc answerable:** {multiDoc} ({100.0 * multiDoc / total:F0}%) ✅ (works in production)");
        sb.AppendLine($"- **Context-dependent:** {contextDep} ({100.0 * contextDep / total:F0}%) ⚠️ (needs rephrasing)");
        sb.AppendLine($"- **Info missing:** {infoMissing} ({100.0 * infoMissing / total:F0}%) ❌ (consider removing)");
        sb.AppendLine();

        var byGroup = evaluations.GroupBy(e => e.Group);

        foreach (var group in byGroup)
        {
            sb.AppendLine($"## {group.Key}");
            sb.AppendLine();

            var byCategory = group.GroupBy(e => e.Category);

            foreach (var category in byCategory)
            {
                sb.AppendLine($"### {category.Key}");
                sb.AppendLine();
                sb.AppendLine("| Query | Category | Prod Valid | Suggested Rephrase |");
                sb.AppendLine("|-------|----------|------------|-------------------|");

                foreach (var (_, _, query, eval) in category)
                {
                    var prodValid = eval.ProductionValid ? "✅" : "❌";
                    var rephrase = eval.SuggestedRephrase ?? "-";
                    var truncatedQuery = query.Length > 50 ? query[..47] + "..." : query;
                    sb.AppendLine($"| {truncatedQuery} | {eval.Category} | {prodValid} | {rephrase} |");
                }

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string GenerateSummary(
        List<(string Group, string Category, string Query, QueryEvaluation Eval)> evaluations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\n=== EVALUATION SUMMARY ===\n");

        var total = evaluations.Count;
        var singleDoc = evaluations.Count(e => e.Eval.Category == "single_doc_answerable");
        var multiDoc = evaluations.Count(e => e.Eval.Category == "multi_doc_answerable");
        var contextDep = evaluations.Count(e => e.Eval.Category == "context_dependent");
        var infoMissing = evaluations.Count(e => e.Eval.Category == "info_missing");

        sb.AppendLine($"Total queries evaluated: {total}");
        sb.AppendLine($"  Single-doc answerable: {singleDoc} ({100.0 * singleDoc / total:F0}%)");
        sb.AppendLine($"  Multi-doc answerable:  {multiDoc} ({100.0 * multiDoc / total:F0}%)");
        sb.AppendLine($"  Context-dependent:     {contextDep} ({100.0 * contextDep / total:F0}%)");
        sb.AppendLine($"  Info missing:          {infoMissing} ({100.0 * infoMissing / total:F0}%)");
        sb.AppendLine();
        sb.AppendLine($"Production-ready queries: {singleDoc + multiDoc} ({100.0 * (singleDoc + multiDoc) / total:F0}%)");

        return sb.ToString();
    }

    #endregion

    #region Models

    private record QueryData(List<QueryGroup> Groups);

    private record QueryGroup(string Title, List<QueryCategory> Categories);

    private record QueryCategory(string Title, List<string> Queries);

    private record QueryEvaluation(
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("testable")] bool Testable,
        [property: JsonPropertyName("productionValid")] bool ProductionValid,
        [property: JsonPropertyName("suggestedRephrase")] string? SuggestedRephrase,
        [property: JsonPropertyName("reason")] string Reason);

    #endregion
}
