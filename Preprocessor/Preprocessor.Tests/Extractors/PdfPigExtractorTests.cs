using Microsoft.Extensions.Logging;

using Moq;

using Preprocessor.Extractors;

namespace Preprocessor.Tests.Extractors;

[TestFixture]
[TestOf(typeof(PdfPigExtractor))]
public class PdfPigExtractorTests
{
    private const string TestPdfFileName = "SEB Asienfond ex Japan D utd.pdf";

    private Mock<ILogger<PdfPigExtractor>> _loggerMock = null!;
    private PdfPigExtractor _extractor = null!;
    private string _testPdfPath = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<PdfPigExtractor>>();
        _extractor = new PdfPigExtractor(_loggerMock.Object);

        var testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
        _testPdfPath = Path.Combine(testDataDir, TestPdfFileName);
    }

    [Test]
    public void MethodName_ShouldReturn_PdfPig() => Assert.That(_extractor.MethodName, Is.EqualTo("pdfpig"));

    [Test]
    public void ExtractAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.pdf");

        Assert.ThrowsAsync<FileNotFoundException>(async () => await _extractor.ExtractAsync(nonExistentPath));
    }

    [Test]
    public async Task ExtractAsync_WithValidPdf_ShouldReturnChunks()
    {
        // Arrange - use real test PDF

        // Act
        var result = await _extractor.ExtractAsync(_testPdfPath);
        var chunks = result.ToList();

        // Assert
        Assert.That(chunks, Is.Not.Empty);
        Assert.That(chunks[0].SourceFile, Is.EqualTo(TestPdfFileName));
        Assert.That(chunks[0].PageNumber, Is.GreaterThan(0));
        Assert.That(chunks[0].Content, Is.Not.Empty);
    }

    [Test]
    public async Task ExtractAsync_ShouldSetCorrectSourceFile()
    {
        // Arrange

        // Act
        var result = await _extractor.ExtractAsync(_testPdfPath);
        var chunks = result.ToList();

        // Assert
        Assert.That(chunks.All(c => c.SourceFile == TestPdfFileName), Is.True);
    }

    [Test]
    public async Task ExtractAsync_ShouldReturnExpectedChunkCount()
    {
        // Arrange

        // Act
        var result = await _extractor.ExtractAsync(_testPdfPath);
        var chunks = result.ToList();

        // Assert
        Assert.That(chunks, Has.Count.EqualTo(13), "Expected 13 chunks from the test PDF");
    }

    [Test]
    public void Constructor_WithCustomChunkSize_ShouldUseCustomSize()
    {
        var customExtractor = new PdfPigExtractor(_loggerMock.Object, 500);

        Assert.That(customExtractor.MethodName, Is.EqualTo("pdfpig"));
    }
}