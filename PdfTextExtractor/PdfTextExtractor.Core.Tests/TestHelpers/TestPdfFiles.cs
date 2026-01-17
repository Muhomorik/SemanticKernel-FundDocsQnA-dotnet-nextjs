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
}
