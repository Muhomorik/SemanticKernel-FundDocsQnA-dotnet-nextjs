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
/// <para><b>GOAL:</b> ALL 3 PAGES must be extracted correctly as proper markdown.</para>
///
/// <para><b>AI AGENT WORKFLOW (only when user explicitly requests):</b></para>
/// <list type="number">
///   <item>Run ALL 3 tests: ExtractPage1, ExtractPage2, ExtractPage3</item>
///   <item>Review the extracted text in the test output for EACH page</item>
///   <item>Check that tables are formatted as proper markdown tables with | delimiters</item>
///   <item>If ANY page has incorrect formatting, edit ONLY the <see cref="ExtractionPrompt"/> constant</item>
///   <item>Repeat until ALL 3 pages produce correct markdown output</item>
/// </list>
///
/// <para><b>SUCCESS CRITERIA - All must pass:</b></para>
/// <list type="bullet">
///   <item>Page 1: Text extracted with proper structure, headings preserved</item>
///   <item>Page 2: Investment scenarios TABLE rendered as markdown table</item>
///   <item>Page 3: Cost breakdown TABLES (multiple) rendered as markdown tables</item>
/// </list>
///
/// <para><b>IMPORTANT CONSTRAINTS:</b></para>
/// <list type="bullet">
///   <item>DO NOT modify <see cref="DefaultModel"/>, <see cref="DefaultMaxTokens"/>, or <see cref="DefaultDetailLevel"/></item>
///   <item>ONLY the <see cref="ExtractionPrompt"/> constant may be changed</item>
///   <item>The API key is loaded from user secrets and must NEVER be logged</item>
/// </list>
///
/// <para><b>Expected markdown table format:</b></para>
/// <code>
/// | Header 1 | Header 2 | Header 3 |
/// |----------|----------|----------|
/// | Data 1   | Data 2   | Data 3   |
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
    private const string ExtractionPrompt = @"Extract ALL text verbatim from this document image. Do NOT describe or summarize - output the exact text as it appears.

CRITICAL RULES:
1. Extract text EXACTLY as written - every word, number, and symbol
2. Format ALL tables as markdown tables using | delimiters:
   | Header 1 | Header 2 | Header 3 |
   |----------|----------|----------|
   | Data 1   | Data 2   | Data 3   |
3. Preserve exact numbers, percentages, and currency values (e.g., 2 698 USD, -73,0%, 1,52%)
4. Exclude only page headers/footers (logos, page numbers, dates)
5. Use ## for section headings
6. Preserve paragraph structure with blank lines between sections";

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
    ///   <item>Investment scenarios TABLE with performance projections</item>
    ///   <item>Rows: Stress, Negativt, Neutralt, Positivt scenarios</item>
    ///   <item>Columns: Time periods (1 år, 5 år) with USD values</item>
    ///   <item>Cost information and fee explanations</item>
    /// </list>
    ///
    /// <para><b>AI Agent Success Criteria - CRITICAL TABLE:</b></para>
    /// <list type="bullet">
    ///   <item>Scenarios table MUST be rendered as markdown table with | delimiters</item>
    ///   <item>All numerical values (USD amounts, percentages) preserved exactly</item>
    ///   <item>Table headers and separator row (|---|---|) present</item>
    /// </list>
    ///
    /// <para><b>Expected table format:</b></para>
    /// <code>
    /// | Scenario | Om du löser in efter 1 år | Om du löser in efter 5 år |
    /// |----------|---------------------------|---------------------------|
    /// | Stress   | 2 698 USD                 | 3 610 USD                 |
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
    /// Extracts text from page 3 of the sample PDF and logs the result.
    ///
    /// <para><b>Page Content:</b></para>
    /// <list type="bullet">
    ///   <item>Cost summary table (Totala kostnader, Årliga kostnadseffekter)</item>
    ///   <item>Detailed cost breakdown table with categories:</item>
    ///   <item>- Engångskostnader (one-time costs): Teckningskostnader, Inlösenkostnader</item>
    ///   <item>- Löpande kostnader (ongoing costs): percentages and USD values</item>
    ///   <item>- Transaktionskostnader, Extra kostnader</item>
    ///   <item>Contact information and legal disclaimers</item>
    /// </list>
    ///
    /// <para><b>AI Agent Success Criteria - MULTIPLE TABLES:</b></para>
    /// <list type="bullet">
    ///   <item>ALL tables MUST be rendered as markdown tables with | delimiters</item>
    ///   <item>Cost percentages (e.g., 2.1%, 1.52%, 0.64%) preserved exactly</item>
    ///   <item>USD values preserved exactly</item>
    ///   <item>Each table has proper headers and separator rows</item>
    /// </list>
    /// </summary>
    [Test]
    public async Task ExtractPage3_WithExtractionPrompt_LogsExtractedText()
    {
        // Arrange
        var imagePath = TestPdfFiles.SamplePdfPage3Image;
        VerifyImageExists(imagePath, "Page 3");

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
