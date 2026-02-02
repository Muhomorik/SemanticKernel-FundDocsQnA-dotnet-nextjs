using AutoFixture;
using AutoFixture.AutoMoq;

using Microsoft.Extensions.Logging;

using Moq;

using Preprocessor.Extractors;
using Preprocessor.Services;
using Preprocessor.Tests.TestHelpers;

namespace Preprocessor.Tests.Extractors;

[TestFixture]
public class TextFileExtractorTests
{
    private IFixture _fixture;
    private Mock<ILogger<TextFileExtractor>> _loggerMock;
    private Mock<ITextChunker> _textChunkerMock;
    private TextFileExtractor _sut;
    private string _tempDirectory;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger<TextFileExtractor>>>();
        _textChunkerMock = _fixture.Freeze<Mock<ITextChunker>>();

        // Create temporary directory for file system tests
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        _sut = _fixture.Create<TextFileExtractor>();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public void MethodName_ShouldReturn_TextFile()
    {
        // Act
        var result = _sut.MethodName;

        // Assert
        Assert.That(result, Is.EqualTo("textfile"));
    }

    [Test]
    public async Task ExtractAsync_WithNoTextFiles_ReturnsEmpty_AndLogsWarning()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDirectory, "document.pdf");
        File.WriteAllText(pdfPath, "dummy"); // Create PDF file

        // Act
        var result = await _sut.ExtractAsync(pdfPath);

        // Assert
        Assert.That(result, Is.Empty);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No text files found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractAsync_WithValidTextFiles_ReturnsChunks()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDirectory, "test_doc.pdf");
        File.WriteAllText(pdfPath, "dummy");

        // Create 3 sequential page files
        File.WriteAllText(Path.Combine(_tempDirectory, "test_doc_page_1.txt"), "Page 1 content");
        File.WriteAllText(Path.Combine(_tempDirectory, "test_doc_page_2.txt"), "Page 2 content");
        File.WriteAllText(Path.Combine(_tempDirectory, "test_doc_page_3.txt"), "Page 3 content");

        // Mock text chunker to return single chunk per page
        _textChunkerMock
            .Setup(x => x.Chunk(It.IsAny<string>()))
            .Returns((string text) => new[] { text });

        // Act
        var result = (await _sut.ExtractAsync(pdfPath)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].SourceFile, Is.EqualTo("test_doc.pdf"));
        Assert.That(result[0].PageNumber, Is.EqualTo(1));
        Assert.That(result[1].PageNumber, Is.EqualTo(2));
        Assert.That(result[2].PageNumber, Is.EqualTo(3));
        _textChunkerMock.Verify(x => x.Chunk(It.IsAny<string>()), Times.Exactly(3));
    }

    [Test]
    public async Task ExtractAsync_WithRealPdfExtraction_CreatesSemanticChunks()
    {
        // Arrange
        var pdfPath = TestFiles.PdfExamplePdf;

        // Use real SemanticChunker
        var realChunker = new SemanticChunker(800, 0.15);
        _sut = new TextFileExtractor(_loggerMock.Object, realChunker);

        // Act
        var result = (await _sut.ExtractAsync(pdfPath)).ToList();

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Count, Is.GreaterThanOrEqualTo(3), "Should create chunks from 3 pages of fund document");

        // Verify all chunks have correct metadata
        foreach (var chunk in result)
        {
            Assert.That(chunk.SourceFile, Is.EqualTo("pdf_example.pdf"));
            Assert.That(chunk.PageNumber, Is.GreaterThanOrEqualTo(1));
            Assert.That(chunk.PageNumber, Is.LessThanOrEqualTo(3));
            Assert.That(chunk.Content, Is.Not.Empty);
        }

        // Verify pages are represented
        var distinctPages = result.Select(c => c.PageNumber).Distinct().OrderBy(p => p).ToList();
        Assert.That(distinctPages, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task ExtractAsync_WithMultiplePages_OrdersCorrectly()
    {
        // Arrange - create files in non-sequential order
        var pdfPath = Path.Combine(_tempDirectory, "ordered.pdf");
        File.WriteAllText(pdfPath, "dummy");

        File.WriteAllText(Path.Combine(_tempDirectory, "ordered_page_3.txt"), "Page 3");
        File.WriteAllText(Path.Combine(_tempDirectory, "ordered_page_1.txt"), "Page 1");
        File.WriteAllText(Path.Combine(_tempDirectory, "ordered_page_2.txt"), "Page 2");

        _textChunkerMock
            .Setup(x => x.Chunk(It.IsAny<string>()))
            .Returns((string text) => new[] { text });

        // Act
        var result = (await _sut.ExtractAsync(pdfPath)).ToList();

        // Assert - should be ordered by page number
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].PageNumber, Is.EqualTo(1));
        Assert.That(result[1].PageNumber, Is.EqualTo(2));
        Assert.That(result[2].PageNumber, Is.EqualTo(3));
    }

    [Test]
    public async Task ExtractAsync_WithNonSequentialPages_SkipsPdf()
    {
        // Arrange - pages 1, 3, 4 (missing 2)
        var pdfPath = Path.Combine(_tempDirectory, "gaps.pdf");
        File.WriteAllText(pdfPath, "dummy");

        File.WriteAllText(Path.Combine(_tempDirectory, "gaps_page_1.txt"), "Page 1");
        File.WriteAllText(Path.Combine(_tempDirectory, "gaps_page_3.txt"), "Page 3");
        File.WriteAllText(Path.Combine(_tempDirectory, "gaps_page_4.txt"), "Page 4");

        // Act
        var result = await _sut.ExtractAsync(pdfPath);

        // Assert
        Assert.That(result, Is.Empty);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Non-sequential")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractAsync_WithPagesNotStartingAtOne_SkipsPdf()
    {
        // Arrange - pages 2, 3, 4 (missing 1)
        var pdfPath = Path.Combine(_tempDirectory, "no_page_1.pdf");
        File.WriteAllText(pdfPath, "dummy");

        File.WriteAllText(Path.Combine(_tempDirectory, "no_page_1_page_2.txt"), "Page 2");
        File.WriteAllText(Path.Combine(_tempDirectory, "no_page_1_page_3.txt"), "Page 3");
        File.WriteAllText(Path.Combine(_tempDirectory, "no_page_1_page_4.txt"), "Page 4");

        // Act
        var result = await _sut.ExtractAsync(pdfPath);

        // Assert
        Assert.That(result, Is.Empty);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Non-sequential")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractAsync_WithEmptyTextFile_SkipsPage()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDirectory, "empty_page.pdf");
        File.WriteAllText(pdfPath, "dummy");

        File.WriteAllText(Path.Combine(_tempDirectory, "empty_page_page_1.txt"), "Page 1 content");
        File.WriteAllText(Path.Combine(_tempDirectory, "empty_page_page_2.txt"), ""); // Empty
        File.WriteAllText(Path.Combine(_tempDirectory, "empty_page_page_3.txt"), "Page 3 content");

        _textChunkerMock
            .Setup(x => x.Chunk(It.IsAny<string>()))
            .Returns((string text) => new[] { text });

        // Act
        var result = (await _sut.ExtractAsync(pdfPath)).ToList();

        // Assert - only 2 chunks (pages 1 and 3)
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].PageNumber, Is.EqualTo(1));
        Assert.That(result[1].PageNumber, Is.EqualTo(3));
    }

    [Test]
    public async Task ExtractAsync_SetsCorrectSourceFile()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDirectory, "source_test.pdf");
        File.WriteAllText(pdfPath, "dummy");

        File.WriteAllText(Path.Combine(_tempDirectory, "source_test_page_1.txt"), "Content");

        _textChunkerMock
            .Setup(x => x.Chunk(It.IsAny<string>()))
            .Returns(new[] { "Chunk 1" });

        // Act
        var result = (await _sut.ExtractAsync(pdfPath)).ToList();

        // Assert - SourceFile should be PDF filename, not text filename
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SourceFile, Is.EqualTo("source_test.pdf"));
        Assert.That(result[0].SourceFile, Does.Not.Contain("_page_"));
    }

    [Test]
    public async Task ExtractAsync_CallsTextChunker_ForEachPage()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDirectory, "chunker_test.pdf");
        File.WriteAllText(pdfPath, "dummy");

        File.WriteAllText(Path.Combine(_tempDirectory, "chunker_test_page_1.txt"), "Page 1");
        File.WriteAllText(Path.Combine(_tempDirectory, "chunker_test_page_2.txt"), "Page 2");

        _textChunkerMock
            .Setup(x => x.Chunk(It.IsAny<string>()))
            .Returns((string text) => new[] { $"Chunk from {text}" });

        // Act
        var result = await _sut.ExtractAsync(pdfPath);

        // Assert
        _textChunkerMock.Verify(x => x.Chunk(It.IsAny<string>()), Times.Exactly(2));
        _textChunkerMock.Verify(x => x.Chunk(It.Is<string>(s => s.Contains("Page 1"))), Times.Once);
        _textChunkerMock.Verify(x => x.Chunk(It.Is<string>(s => s.Contains("Page 2"))), Times.Once);
    }

    [Test]
    public void ExtractAsync_CancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var pdfPath = Path.Combine(_tempDirectory, "cancel_test.pdf");
        File.WriteAllText(pdfPath, "dummy");

        File.WriteAllText(Path.Combine(_tempDirectory, "cancel_test_page_1.txt"), "Content");

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _sut.ExtractAsync(pdfPath, cts.Token));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TextFileExtractor(null!, _textChunkerMock.Object));

        Assert.That(ex.ParamName, Is.EqualTo("logger"));
    }

    [Test]
    public void Constructor_WithNullTextChunker_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TextFileExtractor(_loggerMock.Object, null!));

        Assert.That(ex.ParamName, Is.EqualTo("textChunker"));
    }

    [Test]
    public void ExtractAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.ExtractAsync(null!));

        Assert.That(ex.ParamName, Is.EqualTo("pdfFilePath"));
    }

    [Test]
    public void ExtractAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.ExtractAsync(""));

        Assert.That(ex.ParamName, Is.EqualTo("pdfFilePath"));
    }
}