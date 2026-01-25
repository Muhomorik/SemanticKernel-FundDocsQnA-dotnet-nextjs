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
    public string ExtractionPrompt { get; init; } = @"Extract all text from this page, excluding headers and footers. Preserve line breaks and paragraph structure.

Use markdown formatting:
- Use # for headings and ** for bold text
- For regular paragraphs, preserve line breaks as they appear

Table formatting rules (CRITICAL - follow exactly):
1. If a cell spans multiple columns (merged cell), determine its purpose:
   - If it's a section title/header: Output it as a markdown heading (## or **bold**) BEFORE the table, not as part of the table
   - If it's a subsection within the table: Place it in the first column with other columns empty
2. Every table MUST have a proper header row with column names (infer from context if not explicitly labeled)
3. Add a separator row (|---|---|) immediately after the header row
4. Ensure every data row has the same number of columns as the header
5. If a cell contains multiple lines of text, use <br> to separate lines within the cell
6. Preserve numerical data exactly (currency symbols, percentages, decimal separators)
7. Do NOT create rows with all empty cells - skip them
8. Do NOT use empty column headers - if the original has no headers, infer meaningful names from the data

Example of correct output:
**Section Title Here**

| Column 1 | Column 2 | Column 3 |
|---|---|---|
| Data 1 | Data 2 | Data 3 |

Do not wrap the output in code blocks (no ``` markers). Output only the extracted text with no explanations or commentary.";

    /// <summary>
    /// If true, skip extraction for pages whose text files already exist in the output folder.
    /// Enables incremental/resume extraction. Default is false.
    /// </summary>
    public bool SkipIfExists { get; init; } = false;
}
