namespace PdfTextExtractor.Core.Tests.TestHelpers;

/// <summary>
/// Provides paths to test PDF files.
/// </summary>
public static class TestPdfFiles
{
    /// <summary>
    /// Gets the path to the test PDF file in the TestData folder.
    /// </summary>
    public static string SamplePdf => Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "pdf_for_testing.pdf");

    /// <summary>
    /// Verifies that the test PDF file exists.
    /// </summary>
    public static bool SamplePdfExists => File.Exists(SamplePdf);

    /// <summary>
    /// Gets the path to page 1 image of the example PDF.
    /// </summary>
    /// <remarks>
    /// <para><b>Page Content:</b> Product header (SEB Asienfond ex Japan), risk indicator scale (1-7),
    /// two-column layout with fund description text.</para>
    /// <para><b>AI Agent:</b> Use this page to test general text extraction and two-column layout handling.</para>
    /// </remarks>
    public static string SamplePdfPage1Image => Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "pdf_example_page_001.png");

    /// <summary>
    /// Gets the path to page 2 image of the example PDF.
    /// </summary>
    /// <remarks>
    /// <para><b>Page Content:</b> Investment scenarios TABLE with rows (Stress/Negativt/Neutral/Positiv)
    /// and columns for different time periods (1 år, 5 år). Contains numerical data with currency values.</para>
    /// <para><b>AI Agent:</b> This page has a CRITICAL TABLE. Verify extraction produces proper markdown:
    /// <code>
    /// | Scenario | Om du löser in efter 1 år | Om du löser in efter 5 år |
    /// |----------|---------------------------|---------------------------|
    /// | Stress   | 2 698 USD                 | 3 610 USD                 |
    /// </code>
    /// </para>
    /// </remarks>
    public static string SamplePdfPage2Image => Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "pdf_example_page_002.png");

    /// <summary>
    /// Gets the path to page 3 image of the example PDF.
    /// </summary>
    /// <remarks>
    /// <para><b>Page Content:</b> Cost breakdown TABLES showing fees (Totala kostnader, Årliga kostnadseffekter)
    /// and detailed cost categories (Engångskostnader, Löpande kostnader, etc.) with percentages and USD values.</para>
    /// <para><b>AI Agent:</b> This page has MULTIPLE TABLES. Verify all tables render as proper markdown
    /// with | delimiters, proper headers, and correct numerical values preserved.</para>
    /// </remarks>
    public static string SamplePdfPage3Image => Path.Combine(
        AppContext.BaseDirectory,
        "TestData",
        "pdf_example_page_003.png");
}
