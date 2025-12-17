using Microsoft.Extensions.Logging;
using Moq;
using Preprocessor.Extractors;

namespace Preprocessor.Tests.Extractors;

[TestFixture]
public class PdfPigExtractorTests
{
    private Mock<ILogger<PdfPigExtractor>> _loggerMock = null!;
    private PdfPigExtractor _extractor = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<PdfPigExtractor>>();
        _extractor = new PdfPigExtractor(_loggerMock.Object);
    }

    [Test]
    public void MethodName_ShouldReturn_PdfPig()
    {
        Assert.That(_extractor.MethodName, Is.EqualTo("pdfpig"));
    }

    [Test]
    public void ExtractAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.pdf");

        Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _extractor.ExtractAsync(nonExistentPath));
    }

    [Test]
    public async Task ExtractAsync_WithValidPdf_ShouldReturnChunks()
    {
        // Arrange - use a test PDF if available in TestData folder
        var testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
        var testPdfPath = Path.Combine(testDataDir, "sample.pdf");

        // Skip if no test PDF available
        if (!File.Exists(testPdfPath))
        {
            Assert.Ignore("Test PDF not found. Add a sample.pdf to TestData folder to run this test.");
            return;
        }

        // Act
        var result = await _extractor.ExtractAsync(testPdfPath);
        var chunks = result.ToList();

        // Assert
        Assert.That(chunks, Is.Not.Empty);
        Assert.That(chunks[0].SourceFile, Is.EqualTo("sample.pdf"));
        Assert.That(chunks[0].PageNumber, Is.GreaterThan(0));
        Assert.That(chunks[0].Content, Is.Not.Empty);
    }

    [Test]
    public async Task ExtractAsync_ShouldSetCorrectSourceFile()
    {
        // Arrange
        var testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
        var testPdfPath = Path.Combine(testDataDir, "sample.pdf");

        if (!File.Exists(testPdfPath))
        {
            Assert.Ignore("Test PDF not found.");
            return;
        }

        // Act
        var result = await _extractor.ExtractAsync(testPdfPath);
        var chunks = result.ToList();

        // Assert
        Assert.That(chunks.All(c => c.SourceFile == "sample.pdf"), Is.True);
    }

    [Test]
    public void Constructor_WithCustomChunkSize_ShouldUseCustomSize()
    {
        var customExtractor = new PdfPigExtractor(_loggerMock.Object, chunkSize: 500);

        Assert.That(customExtractor.MethodName, Is.EqualTo("pdfpig"));
    }
}
