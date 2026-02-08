using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PdfTextExtractor.Core.Infrastructure.OpenAI;
using PdfTextExtractor.Core.Models;
using PdfTextExtractor.Core.Tests.TestHelpers;

namespace PdfTextExtractor.Core.Tests.Integration;

/// <summary>
/// Integration tests for OpenAI Vision API text extraction.
/// These tests call the real OpenAI API to extract text from PDF page images.
///
/// <para><b>!!! AI AGENT - DO NOT RUN TESTS AUTOMATICALLY !!!</b></para>
/// <para>
/// These tests make real API calls to OpenAI and incur costs.
/// The user MUST explicitly request running these tests.
/// Do NOT run tests unless user says "run tests", "go", or similar explicit command.
/// </para>
///
/// <para><b>GOAL:</b> Extract text optimized for RAG (Retrieval-Augmented Generation).</para>
///
/// <para><b>AI AGENT WORKFLOW (only when user explicitly requests):</b></para>
/// <list type="number">
///   <item>Run tests: ExtractPage1, ExtractPage2, ExtractPage3_Table1, ExtractPage3_Table2</item>
///   <item>Review the extracted text in the test output for EACH page</item>
///   <item>Check that ALL tables are LINEARIZED to natural language (NOT markdown tables)</item>
///   <item>If ANY page has incorrect formatting, edit ONLY the <see cref="ExtractionPrompt"/> constant</item>
///   <item>Repeat until pages produce correct output</item>
/// </list>
///
/// <para><b>SUCCESS CRITERIA - All must pass:</b></para>
/// <list type="bullet">
///   <item>Page 1: Text extracted with proper structure, headings preserved</item>
///   <item>Page 2: Performance scenarios LINEARIZED to natural language</item>
///   <item>Page 3 Table 1: Cost summary LINEARIZED to natural language</item>
///   <item>Page 3 Table 2: Cost breakdown LINEARIZED to natural language</item>
/// </list>
///
/// <para><b>IMPORTANT CONSTRAINTS:</b></para>
/// <list type="bullet">
///   <item>DO NOT modify <see cref="DefaultModel"/>, <see cref="DefaultMaxTokens"/>, or <see cref="DefaultDetailLevel"/></item>
///   <item>ONLY the <see cref="ExtractionPrompt"/> constant may be changed</item>
///   <item>The API key is loaded from user secrets and must NEVER be logged</item>
/// </list>
///
/// <para><b>Expected linearized format for complex tables (performance scenarios):</b></para>
/// <code>
/// Stressscenario:
/// - Efter 1 år: 2 698 USD (genomsnittlig avkastning -73,0% per år)
/// - Efter 5 år: 3 610 USD (genomsnittlig avkastning -18,4% per år)
/// </code>
///
/// <para><b>How to run these tests:</b></para>
/// <code>
/// dotnet test --filter "FullyQualifiedName~OpenAIVisionClientIntegrationTests" PdfTextExtractor.Core.Tests --logger "console;verbosity=detailed"
/// </code>
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("OpenAI")]
[Explicit("Requires OpenAI API key in user secrets (BackendOptions:OpenAIApiKey)")]
public class OpenAIVisionClientIntegrationTests
{
    // ============================================================
    // FIXED PARAMETERS - DO NOT MODIFY
    // These match OpenAIParameters defaults and must not be changed
    // ============================================================
    private const string DefaultModel = "gpt-4o";
    private const int DefaultMaxTokens = 2000;
    private const string DefaultDetailLevel = "high";

    // ============================================================
    // EXTRACTION PROMPT - MODIFY THIS TO IMPROVE EXTRACTION
    // This is the ONLY constant that should be changed during iteration
    // ============================================================
    private const string ExtractionPrompt = @"Extract ALL text from this document image. Output is optimized for RAG (semantic search).

CRITICAL RULES:
1. Extract text EXACTLY as written - every word, number, and symbol
2. Preserve exact numbers, percentages, and currency values (e.g., 2 698 USD, -73,0%, 1,52%)
3. Exclude only page headers/footers (logos, page numbers, dates)
4. Use ## for section headings
5. Preserve paragraph structure with blank lines between sections

TABLE LINEARIZATION (CRITICAL for RAG):
- Do NOT use markdown tables with | delimiters
- LINEARIZE ALL tables into natural language sentences
- For tables with time periods (1 år, 5 år), include the time period in EACH bullet point

PERFORMANCE SCENARIO TABLE FORMAT (MUST follow exactly):
When you see a table with scenarios (Stress, Negativt, Neutralt, Positivt) and columns for time periods:
- Add 'scenario' suffix to each scenario name
- Each bullet MUST include the time period label
- Put the percentage in parentheses with full context

Example output format:
Stressscenario:
- Efter 1 år: 2 698 USD (genomsnittlig avkastning -73,0% per år)
- Efter 5 år: 3 610 USD (genomsnittlig avkastning -18,4% per år)

Negativt scenario:
- Efter 1 år: 6 693 USD (genomsnittlig avkastning -33,1% per år)
- Efter 5 år: 7 054 USD (genomsnittlig avkastning -6,7% per år)

COST TABLE FORMAT:
For cost breakdown tables, use heading followed by bullet points with description and amount.";

    private string _apiKey = null!;
    private HttpClient _httpClient = null!;
    private OpenAIVisionClient _sut = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Load API key from user secrets
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<OpenAIVisionClientIntegrationTests>()
            .Build();

        _apiKey = configuration["BackendOptions:OpenAIApiKey"] ?? string.Empty;

        // Log whether key was loaded (NEVER log the actual key)
        var keyLoaded = !string.IsNullOrWhiteSpace(_apiKey);
        TestContext.WriteLine($"API key loaded from user secrets: {keyLoaded}");

        if (!keyLoaded)
        {
            Assert.Ignore(
                "OpenAI API key not found in user secrets.\n" +
                "Set it using: dotnet user-secrets set \"BackendOptions:OpenAIApiKey\" \"sk-...\"");
        }
    }

    [SetUp]
    public void SetUp()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        var logger = new NullLogger<OpenAIVisionClient>();
        _sut = new OpenAIVisionClient(_httpClient, logger, ExtractionPrompt);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    /// <summary>
    /// Extracts text from page 1 of the sample PDF and logs the result.
    ///
    /// <para><b>Page Content:</b></para>
    /// <list type="bullet">
    ///   <item>Product header: "Faktablad" with SEB logo</item>
    ///   <item>Fund name: SEB Asienfond ex Japan - Andelsklass D Utdelande (USD)</item>
    ///   <item>Risk indicator scale (1-7) with value 5</item>
    ///   <item>Two-column layout with Swedish text describing the fund</item>
    /// </list>
    ///
    /// <para><b>AI Agent Success Criteria:</b></para>
    /// <list type="bullet">
    ///   <item>Headings preserved (Produkt, Vad innebär produkten?, etc.)</item>
    ///   <item>Text from both columns extracted and readable</item>
    ///   <item>Risk indicator section formatted clearly</item>
    /// </list>
    /// </summary>
    [Test]
    public async Task ExtractPage1_WithExtractionPrompt_LogsExtractedText()
    {
        // Arrange
        var imagePath = TestPdfFiles.SamplePdfPage1Image;
        VerifyImageExists(imagePath, "Page 1");

        // Act
        var result = await ExtractAndLogAsync(imagePath, pageNumber: 1);

        // Assert
        Assert.That(result.ExtractedText, Is.Not.Null.And.Not.Empty,
            "Extraction should return text content");
    }

    /// <summary>
    /// Extracts text from page 2 of the sample PDF and logs the result.
    ///
    /// <para><b>Page Content:</b></para>
    /// <list type="bullet">
    ///   <item>Investment scenarios with performance projections</item>
    ///   <item>Scenarios: Stress, Negativt, Neutralt, Positivt</item>
    ///   <item>Time periods: 1 år, 5 år with USD values and percentages</item>
    ///   <item>Cost information and fee explanations</item>
    /// </list>
    ///
    /// <para><b>AI Agent Success Criteria - LINEARIZATION:</b></para>
    /// <list type="bullet">
    ///   <item>Performance scenarios MUST be LINEARIZED to natural language (NOT markdown tables)</item>
    ///   <item>Each scenario as a heading followed by bullet points for time periods</item>
    ///   <item>All numerical values (USD amounts, percentages) preserved exactly</item>
    ///   <item>Format optimized for RAG semantic search</item>
    /// </list>
    ///
    /// <para><b>Expected linearized format:</b></para>
    /// <code>
    /// Stressscenario:
    /// - Efter 1 år: 2 698 USD (genomsnittlig avkastning -73,0% per år)
    /// - Efter 5 år: 3 610 USD (genomsnittlig avkastning -18,4% per år)
    ///
    /// Negativt scenario:
    /// - Efter 1 år: 6 693 USD (genomsnittlig avkastning -33,1% per år)
    /// - Efter 5 år: 7 054 USD (genomsnittlig avkastning -6,7% per år)
    /// </code>
    /// </summary>
    [Test]
    public async Task ExtractPage2_WithExtractionPrompt_LogsExtractedText()
    {
        // Arrange
        var imagePath = TestPdfFiles.SamplePdfPage2Image;
        VerifyImageExists(imagePath, "Page 2");

        // Act
        var result = await ExtractAndLogAsync(imagePath, pageNumber: 2);

        // Assert
        Assert.That(result.ExtractedText, Is.Not.Null.And.Not.Empty,
            "Extraction should return text content");
    }

    /// <summary>
    /// Extracts text from page 3 of the sample PDF - validates Table 1 (Cost Summary).
    ///
    /// <para><b>Table 1 Content - Cost Summary:</b></para>
    /// <list type="bullet">
    ///   <item>Totala kostnader: 205 USD (1 år), 1 004 USD (5 år)</item>
    ///   <item>Årliga kostnadseffekter: 2,1% for both periods</item>
    /// </list>
    ///
    /// <para><b>AI Agent Success Criteria - LINEARIZATION:</b></para>
    /// <list type="bullet">
    ///   <item>LINEARIZE this table to natural language (NOT markdown tables)</item>
    ///   <item>Cost percentages (2,1%) preserved exactly</item>
    ///   <item>USD values (205 USD, 1 004 USD) preserved exactly</item>
    /// </list>
    ///
    /// <para><b>Expected linearized format:</b></para>
    /// <code>
    /// Totala kostnader:
    /// - Om du löser in efter 1 år: 205 USD
    /// - Om du löser in efter 5 år: 1 004 USD
    ///
    /// Årliga kostnadseffekter: 2,1%
    /// </code>
    /// </summary>
    [Test]
    public async Task ExtractPage3_Table1_CostSummary_LogsExtractedText()
    {
        // Arrange
        var imagePath = TestPdfFiles.SamplePdfPage3Image;
        VerifyImageExists(imagePath, "Page 3 - Table 1");

        // Act
        var result = await ExtractAndLogAsync(imagePath, pageNumber: 3);

        // Assert
        Assert.That(result.ExtractedText, Is.Not.Null.And.Not.Empty,
            "Extraction should return text content");
    }

    /// <summary>
    /// Extracts text from page 3 of the sample PDF - validates Table 2 (Cost Breakdown).
    ///
    /// <para><b>Table 2 Content - Detailed Cost Breakdown:</b></para>
    /// <list type="bullet">
    ///   <item>Engångskostnader (one-time): Teckningskostnader (0 USD), Inlösenkostnader (0 USD)</item>
    ///   <item>Löpande kostnader (ongoing): Förvaltningsavgifter (1,52%, 152 USD), Transaktionskostnader (0,54%, 54 USD)</item>
    ///   <item>Extra kostnader: Resultatrelaterade avgifter (0 USD)</item>
    /// </list>
    ///
    /// <para><b>AI Agent Success Criteria - LINEARIZATION:</b></para>
    /// <list type="bullet">
    ///   <item>LINEARIZE this table - it has long descriptions in cells</item>
    ///   <item>Each cost category as a heading with description and value</item>
    ///   <item>Cost percentages (1,52%, 0,54%) preserved exactly</item>
    ///   <item>USD values (152 USD, 54 USD, 0 USD) preserved exactly</item>
    /// </list>
    ///
    /// <para><b>Expected linearized format:</b></para>
    /// <code>
    /// Engångskostnader vid teckning eller inlösen:
    ///
    /// Teckningskostnader:
    /// Vi tar inte ut någon teckningsavgift, men personen som säljer produkten till dig kan komma att göra det.
    /// Kostnad: 0 USD
    ///
    /// Löpande kostnader som tas ut varje år:
    ///
    /// Förvaltningsavgifter och andra administrations- eller driftskostnader:
    /// 1,52% av värdet på din investering per år. Detta är en uppskattning baserad på faktiska kostnader under det senaste året.
    /// Kostnad: 152 USD
    /// </code>
    /// </summary>
    [Test]
    public async Task ExtractPage3_Table2_CostBreakdown_LogsExtractedText()
    {
        // Arrange
        var imagePath = TestPdfFiles.SamplePdfPage3Image;
        VerifyImageExists(imagePath, "Page 3 - Table 2");

        // Act
        var result = await ExtractAndLogAsync(imagePath, pageNumber: 3);

        // Assert
        Assert.That(result.ExtractedText, Is.Not.Null.And.Not.Empty,
            "Extraction should return text content");
    }

    #region Helper Methods

    private void VerifyImageExists(string imagePath, string pageDescription)
    {
        if (!File.Exists(imagePath))
        {
            Assert.Ignore($"Test image not found for {pageDescription}: {imagePath}");
        }
    }

    private async Task<VisionExtractionResult> ExtractAndLogAsync(string imagePath, int pageNumber)
    {
        // Log test configuration
        TestContext.WriteLine($"=== OpenAI Vision Extraction: Page {pageNumber} ===");
        TestContext.WriteLine($"Model: {DefaultModel}");
        TestContext.WriteLine($"Detail Level: {DefaultDetailLevel}");
        TestContext.WriteLine($"Max Tokens: {DefaultMaxTokens}");
        TestContext.WriteLine();

        // Log the extraction prompt being used
        TestContext.WriteLine("--- EXTRACTION PROMPT ---");
        TestContext.WriteLine(ExtractionPrompt);
        TestContext.WriteLine("--- EXTRACTION PROMPT END ---");
        TestContext.WriteLine();

        // Perform extraction
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = await _sut.ExtractTextFromImageAsync(
            imagePath,
            _apiKey,
            DefaultModel,
            DefaultMaxTokens,
            DefaultDetailLevel);

        stopwatch.Stop();

        // Log the extracted text
        TestContext.WriteLine("--- EXTRACTED TEXT START ---");
        TestContext.WriteLine(result.ExtractedText);
        TestContext.WriteLine("--- EXTRACTED TEXT END ---");
        TestContext.WriteLine();

        // Log token usage and timing
        TestContext.WriteLine($"Token Usage: Prompt={result.PromptTokens}, Completion={result.CompletionTokens}, Total={result.TotalTokens}");
        TestContext.WriteLine($"Extraction Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        TestContext.WriteLine();
        TestContext.WriteLine("Extraction completed successfully.");

        return result;
    }

    #endregion
}
