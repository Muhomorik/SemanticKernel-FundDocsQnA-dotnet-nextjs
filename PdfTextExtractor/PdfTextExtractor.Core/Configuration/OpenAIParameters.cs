namespace PdfTextExtractor.Core.Configuration;

/// <summary>
/// Parameters for OpenAI vision-based OCR text extraction.
/// </summary>
public class OpenAIParameters
{
    public required string PdfFolderPath { get; init; }
    public required string OutputFolderPath { get; init; }
    public required string ApiKey { get; init; }

    /// <summary>
    /// The OpenAI vision model to use for OCR text extraction.
    /// Default is "gpt-4o".
    /// </summary>
    /// <remarks>
    /// Supported vision models include:
    /// <list type="bullet">
    ///     <listheader>
    ///         <term>Model</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term>gpt-4o</term>
    ///         <description>Standard multimodal model (default)</description>
    ///     </item>
    ///     <item>
    ///         <term>gpt-4o-mini</term>
    ///         <description>Smaller, faster, cheaper version with good OCR performance</description>
    ///     </item>
    ///     <item>
    ///         <term>gpt-4-turbo</term>
    ///         <description>Specialized for visual tasks, OCR, and diagram interpretation</description>
    ///     </item>
    ///     <item>
    ///         <term>gpt-4.1</term>
    ///         <description>Latest model with excellent OCR and complex instruction-following</description>
    ///     </item>
    ///     <item>
    ///         <term>gpt-4.5</term>
    ///         <description>Newer GPT-4 series with balanced performance</description>
    ///     </item>
    ///     <item>
    ///         <term>o1, o1-mini, o3-mini</term>
    ///         <description>Reasoning models for complex documents</description>
    ///     </item>
    /// </list>
    /// For cost optimization, try <c>gpt-4o-mini</c> first. For enhanced OCR accuracy, use <c>gpt-4.1</c> or <c>gpt-4-turbo</c>.
    /// Model availability may vary by region and API tier.
    /// </remarks>
    public string VisionModelName { get; init; } = "gpt-4o";

    public int RasterizationDpi { get; init; } = 150;
    public string DetailLevel { get; init; } = "high"; // "low", "high", or "auto"

    /// <summary>
    /// Maximum number of tokens the model can generate in the completion/output for each page.
    /// GPT-4o supports up to 16,384 max output tokens, but defaults to 4,096 if not specified.
    /// Set higher values (e.g., 2000-4000) for dense/complex documents to avoid truncated text.
    /// Lower values reduce API costs but may cut off text mid-extraction.
    /// See: https://platform.openai.com/docs/api-reference/chat
    /// </summary>
    public int MaxTokens { get; init; } = 2000;

    /// <summary>
    /// Prompt sent to the vision model for text extraction.
    /// </summary>
    public string ExtractionPrompt { get; init; } =
        @"Extract ALL text from this document image. Output is optimized for RAG (semantic search).

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

    /// <summary>
    /// If true, skip extraction for pages whose text files already exist in the output folder.
    /// Enables incremental/resume extraction. Default is false.
    /// </summary>
    public bool SkipIfExists { get; init; } = false;
}