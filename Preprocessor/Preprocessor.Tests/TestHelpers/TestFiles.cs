namespace Preprocessor.Tests.TestHelpers;

/// <summary>
/// Provides paths to test files in the TestData folder.
/// </summary>
public static class TestFiles
{
    private static string TestDataDirectory => Path.Combine(
        AppContext.BaseDirectory,
        "TestData");

    /// <summary>
    /// Gets the path to the full extracted text from the example PDF.
    /// </summary>
    /// <remarks>
    /// <para><b>Content:</b> Complete extracted text from pdf_example.pdf containing
    /// fund factsheet information (SEB Asienfond ex Japan).</para>
    /// <para><b>Use case:</b> Test chunking, text processing, and content preservation.</para>
    /// </remarks>
    public static string PdfExampleText => Path.Combine(TestDataDirectory, "pdf_example.txt");

    /// <summary>
    /// Gets the path to page 1 text from the example PDF.
    /// </summary>
    /// <remarks>
    /// <para><b>Content:</b> Extracted text from <see cref="PdfTextExtractor.Core.Tests.TestHelpers.TestPdfFiles.SamplePdfPage1Image"/>.
    /// Product header (SEB Asienfond ex Japan), risk indicator scale (1-7),
    /// two-column layout with fund description.</para>
    /// <para><b>Use case:</b> Test general text extraction and layout handling.</para>
    /// </remarks>
    public static string PdfExamplePage1Text => Path.Combine(TestDataDirectory, "pdf_example _page_1.txt");

    /// <summary>
    /// Gets the path to page 2 text from the example PDF.
    /// </summary>
    /// <remarks>
    /// <para><b>Content:</b> Extracted text from <see cref="PdfTextExtractor.Core.Tests.TestHelpers.TestPdfFiles.SamplePdfPage2Image"/>.
    /// Investment scenarios table with rows (Stress/Negativt/Neutral/Positiv)
    /// and columns for different time periods.</para>
    /// <para><b>Use case:</b> Test table extraction and markdown formatting.</para>
    /// </remarks>
    public static string PdfExamplePage2Text => Path.Combine(TestDataDirectory, "pdf_example_page_2.txt");

    /// <summary>
    /// Gets the path to page 3 text from the example PDF.
    /// </summary>
    /// <remarks>
    /// <para><b>Content:</b> Extracted text from <see cref="PdfTextExtractor.Core.Tests.TestHelpers.TestPdfFiles.SamplePdfPage3Image"/>.
    /// Cost breakdown tables showing fees and detailed cost categories
    /// with percentages and currency values.</para>
    /// <para><b>Use case:</b> Test multiple table extraction and numerical value preservation.</para>
    /// </remarks>
    public static string PdfExamplePage3Text => Path.Combine(TestDataDirectory, "pdf_example_page_3.txt");

    /// <summary>
    /// Gets the path to the example PDF file.
    /// </summary>
    public static string PdfExamplePdf => Path.Combine(TestDataDirectory, "pdf_example.pdf");

    /// <summary>
    /// Gets the path to the SEB Asienfond PDF file.
    /// </summary>
    public static string SebAsienfondPdf => Path.Combine(TestDataDirectory, "SEB Asienfond ex Japan D utd.pdf");

    /// <summary>
    /// Verifies that the specified test file exists.
    /// </summary>
    public static bool Exists(string path) => File.Exists(path);
}
