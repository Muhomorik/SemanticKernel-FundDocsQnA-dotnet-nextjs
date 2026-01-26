using AutoFixture;
using AutoFixture.AutoMoq;

using Microsoft.Extensions.Logging;

using Moq;

using Preprocessor.Extractors;
using Preprocessor.Services;

using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Preprocessor.Tests.Extractors;

[TestFixture]
[TestOf(typeof(PdfPigExtractor))]
public class PdfPigExtractorTests
{
    private const string TestPdfFileName = "SEB Asienfond ex Japan D utd.pdf";

    private IFixture _fixture = null!;
    private Mock<ILogger<PdfPigExtractor>> _loggerMock = null!;
    private Mock<ITextChunker> _textChunkerMock = null!;
    private PdfPigExtractor _extractor = null!;
    private string _testPdfPath = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger<PdfPigExtractor>>>();
        _textChunkerMock = _fixture.Freeze<Mock<ITextChunker>>();

        // For integration tests, we use real SentenceBoundaryChunker
        // For unit tests with mocks, we'll create extractor manually
        _extractor = new PdfPigExtractor(_loggerMock.Object, new SentenceBoundaryChunker(maxChunkSize: 1000));

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
    public async Task ExtractAsync_WithValidPdf_CallsTextChunker()
    {
        // Arrange
        var mockChunks = new[] { "chunk1", "chunk2", "chunk3" };
        _textChunkerMock
            .Setup(x => x.Chunk(It.IsAny<string>()))
            .Returns(mockChunks);

        var extractorWithMock = new PdfPigExtractor(_loggerMock.Object, _textChunkerMock.Object);

        // Act
        var result = await extractorWithMock.ExtractAsync(_testPdfPath);
        var chunks = result.ToList();

        // Assert
        Assert.That(chunks, Is.Not.Empty);
        _textChunkerMock.Verify(x => x.Chunk(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var textChunker = new SentenceBoundaryChunker();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PdfPigExtractor(null!, textChunker));
    }

    [Test]
    public void Constructor_WithNullTextChunker_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = _loggerMock.Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PdfPigExtractor(logger, null!));
    }
}