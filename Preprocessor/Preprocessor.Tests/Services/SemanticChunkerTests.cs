using AutoFixture;
using AutoFixture.AutoMoq;

using Preprocessor.Services;

namespace Preprocessor.Tests.Services;

[TestFixture]
public class SemanticChunkerTests
{
    private const string TestPdfFileName = "pdf_example.txt";

    private IFixture _fixture;
    private SemanticChunker _sut;
    private string _testPdfPath;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _sut = new SemanticChunker(maxChunkSize: 800, overlapPercentage: 0.15);

        // Set up path to test PDF
        var testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
        _testPdfPath = Path.Combine(testDataDir, TestPdfFileName);
    }

    [Test]
    public void Chunk_EmptyText_ReturnsEmptyCollection()
    {
        // Arrange
        var emptyText = string.Empty;

        // Act
        var result = _sut.Chunk(emptyText).ToList();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Chunk_NullText_ReturnsEmptyCollection()
    {
        // Arrange
        string? nullText = null;

        // Act
        var result = _sut.Chunk(nullText!).ToList();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Chunk_SingleParagraph_ReturnsSingleChunk()
    {
        // Arrange
        var singleParagraph = "This is a single paragraph with multiple sentences. It should stay together.";

        // Act
        var result = _sut.Chunk(singleParagraph).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(singleParagraph));
    }

    [Test]
    public void Chunk_MultipleParagraphsShorterThanChunkSize_ReturnsSingleChunk()
    {
        // Arrange
        var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

        // Act
        var result = _sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("First paragraph.\n\nSecond paragraph.\n\nThird paragraph."));
    }

    [Test]
    public void Chunk_TextExceedingChunkSize_SplitsOnParagraphBoundaries()
    {
        // Arrange
        var sut = new SemanticChunker(maxChunkSize: 50, overlapPercentage: 0.15);
        var text = "First paragraph with some content.\n\nSecond paragraph with more content.\n\nThird paragraph.";

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.GreaterThan(1));
        foreach (var chunk in result)
        {
            Assert.That(chunk, Is.Not.Empty);
        }
    }

    [Test]
    public void Chunk_WithOverlap_ContainsOverlappingContent()
    {
        // Arrange
        var sut = new SemanticChunker(maxChunkSize: 60, overlapPercentage: 0.2);
        var text = "Paragraph one with text.\n\nParagraph two with text.\n\nParagraph three with text.\n\nParagraph four with text.\n\nParagraph five with text.";

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.GreaterThan(1));

        // Verify overlap: content from end of chunk N appears at start of chunk N+1
        for (var i = 0; i < result.Count - 1; i++)
        {
            var currentChunk = result[i];
            var nextChunk = result[i + 1];

            // Extract last paragraph(s) from current chunk
            var currentParagraphs = currentChunk.Split(new[] { "\n\n" }, StringSplitOptions.None);
            var lastParagraph = currentParagraphs[^1];

            // Verify it appears in next chunk
            Assert.That(nextChunk, Does.Contain(lastParagraph),
                $"Chunk {i + 1} should contain overlapping content from chunk {i}");
        }
    }

    [Test]
    public void Chunk_HandlesWindowsLineEndings_SplitsCorrectly()
    {
        // Arrange
        var text = "First paragraph.\r\n\r\nSecond paragraph.\r\n\r\nThird paragraph.";

        // Act
        var result = _sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("First paragraph"));
        Assert.That(result[0], Does.Contain("Second paragraph"));
        Assert.That(result[0], Does.Contain("Third paragraph"));
    }

    [Test]
    public void Chunk_HandlesUnixLineEndings_SplitsCorrectly()
    {
        // Arrange
        var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

        // Act
        var result = _sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("First paragraph"));
        Assert.That(result[0], Does.Contain("Second paragraph"));
        Assert.That(result[0], Does.Contain("Third paragraph"));
    }

    [Test]
    public void Chunk_SingleParagraphExceedingMaxSize_ReturnsAsIs()
    {
        // Arrange
        var sut = new SemanticChunker(maxChunkSize: 50, overlapPercentage: 0.15);
        var longParagraph = new string('a', 100);

        // Act
        var result = sut.Chunk(longParagraph).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(longParagraph));
    }

    [Test]
    public void Chunk_MultipleLargeParagraphs_ReturnsEachSeparately()
    {
        // Arrange
        var sut = new SemanticChunker(maxChunkSize: 50, overlapPercentage: 0.15);
        var paragraph1 = new string('a', 100);
        var paragraph2 = new string('b', 100);
        var text = $"{paragraph1}\n\n{paragraph2}";

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(paragraph1));
        Assert.That(result[1], Is.EqualTo(paragraph2));
    }

    [Test]
    public void Chunk_SkipsEmptyParagraphs()
    {
        // Arrange
        var text = "First paragraph.\n\n\n\nSecond paragraph.\n\n\n\nThird paragraph.";

        // Act
        var result = _sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("First paragraph.\n\nSecond paragraph.\n\nThird paragraph."));
    }

    [Test]
    public void Chunk_TrimsWhitespaceFromParagraphs()
    {
        // Arrange
        var text = "  First paragraph.  \n\n  Second paragraph.  \n\n  Third paragraph.  ";

        // Act
        var result = _sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("First paragraph.\n\nSecond paragraph.\n\nThird paragraph."));
    }

    [Test]
    public void Chunk_PreservesParagraphStructure()
    {
        // Arrange
        var sut = new SemanticChunker(maxChunkSize: 200, overlapPercentage: 0.15);
        var text = "Paragraph one.\n\nParagraph two.\n\nParagraph three is longer with more content.\n\nParagraph four.";

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        foreach (var chunk in result)
        {
            // Verify paragraphs are separated by double newlines within chunks
            if (chunk.Contains("\n\n"))
            {
                var parts = chunk.Split(new[] { "\n\n" }, StringSplitOptions.None);
                Assert.That(parts, Has.All.Not.Empty);
            }
        }
    }

    [Test]
    public void Chunk_RealWorldPdfDocument_CreatesSemanticChunks()
    {
        // Arrange
        Assert.That(File.Exists(_testPdfPath), Is.True, $"Test file not found: {_testPdfPath}");
        var pdfText = File.ReadAllText(_testPdfPath);
        var sut = new SemanticChunker(maxChunkSize: 1000, overlapPercentage: 0.15);

        // Act
        var result = sut.Chunk(pdfText).ToList();

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.GreaterThan(1), "Should create multiple chunks for real document");

        // Verify chunks respect max size (allowing some tolerance for semantic boundaries)
        foreach (var chunk in result)
        {
            Assert.That(chunk.Length, Is.LessThanOrEqualTo(1200),
                "Chunk should not significantly exceed max size");
        }

        // Verify semantic sections stay together
        var allText = string.Join(" ", result);
        Assert.That(allText, Does.Contain("SEB Asienfond ex Japan"));
        Assert.That(allText, Does.Contain("Vilka är kostnaderna"));
        Assert.That(allText, Does.Contain("Riskindikator"));
    }

    [Test]
    public void Chunk_RealWorldPdfDocument_HasOverlapBetweenChunks()
    {
        // Arrange
        Assert.That(File.Exists(_testPdfPath), Is.True, $"Test file not found: {_testPdfPath}");
        var pdfText = File.ReadAllText(_testPdfPath);
        var sut = new SemanticChunker(maxChunkSize: 800, overlapPercentage: 0.2);

        // Act
        var result = sut.Chunk(pdfText).ToList();

        // Assert
        Assert.That(result, Has.Count.GreaterThan(1));

        // Verify overlap exists between consecutive chunks
        var overlapFound = false;
        for (var i = 0; i < result.Count - 1; i++)
        {
            var currentChunk = result[i];
            var nextChunk = result[i + 1];

            // Check if any substantial text from end of current chunk appears in next chunk
            var currentParagraphs = currentChunk.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (currentParagraphs.Length > 0)
            {
                var lastParagraph = currentParagraphs[^1];
                if (nextChunk.Contains(lastParagraph))
                {
                    overlapFound = true;
                    break;
                }
            }
        }

        Assert.That(overlapFound, Is.True, "Should have overlap between consecutive chunks");
    }

    [Test]
    public void Chunk_RealWorldPdfDocument_PreservesKeyInformation()
    {
        // Arrange
        Assert.That(File.Exists(_testPdfPath), Is.True, $"Test file not found: {_testPdfPath}");
        var pdfText = File.ReadAllText(_testPdfPath);
        var sut = new SemanticChunker(maxChunkSize: 800, overlapPercentage: 0.15);

        // Act
        var result = sut.Chunk(pdfText).ToList();

        // Assert
        var allChunksText = string.Join(" ", result);

        // Verify critical information is preserved
        Assert.That(allChunksText, Does.Contain("ISIN-kod: SE0021150174"));
        Assert.That(allChunksText, Does.Contain("1,52%")); // Management fees
        Assert.That(allChunksText, Does.Contain("Rekommenderad innehavstid: 5 år"));

        // Verify no content was lost (character count should be close, accounting for overlap)
        var originalLength = pdfText.Length;
        var chunkedLength = allChunksText.Length;

        // Chunked text will be longer due to overlap, but not excessively so
        Assert.That(chunkedLength, Is.GreaterThan(originalLength));
        Assert.That(chunkedLength, Is.LessThan(originalLength * 1.5),
            "Overlap should not cause excessive text duplication");
    }

    [Test]
    public void Constructor_ZeroChunkSize_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new SemanticChunker(maxChunkSize: 0));
        Assert.That(ex.ParamName, Is.EqualTo("maxChunkSize"));
        Assert.That(ex.Message, Does.Contain("Chunk size must be greater than zero"));
    }

    [Test]
    public void Constructor_NegativeChunkSize_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new SemanticChunker(maxChunkSize: -100));
        Assert.That(ex.ParamName, Is.EqualTo("maxChunkSize"));
        Assert.That(ex.Message, Does.Contain("Chunk size must be greater than zero"));
    }

    [Test]
    public void Constructor_NegativeOverlapPercentage_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SemanticChunker(maxChunkSize: 1000, overlapPercentage: -0.1));
        Assert.That(ex.ParamName, Is.EqualTo("overlapPercentage"));
        Assert.That(ex.Message, Does.Contain("must be between 0.0 and 0.5"));
    }

    [Test]
    public void Constructor_OverlapPercentageGreaterThan50_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SemanticChunker(maxChunkSize: 1000, overlapPercentage: 0.6));
        Assert.That(ex.ParamName, Is.EqualTo("overlapPercentage"));
        Assert.That(ex.Message, Does.Contain("must be between 0.0 and 0.5"));
    }

    [Test]
    public void Constructor_ZeroOverlap_CreatesChunksWithoutOverlap()
    {
        // Arrange
        var sut = new SemanticChunker(maxChunkSize: 60, overlapPercentage: 0.0);
        var text = "Paragraph one with text.\n\nParagraph two with text.\n\nParagraph three with text.\n\nParagraph four with text.";

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.GreaterThan(1));

        // Verify no overlap: content from chunk N should not appear in chunk N+1
        for (var i = 0; i < result.Count - 1; i++)
        {
            var currentChunk = result[i];
            var nextChunk = result[i + 1];

            // Since there's no overlap, the last paragraph of current chunk
            // should not be the first paragraph of next chunk
            var currentParagraphs = currentChunk.Split(new[] { "\n\n" }, StringSplitOptions.None);
            var nextParagraphs = nextChunk.Split(new[] { "\n\n" }, StringSplitOptions.None);

            if (currentParagraphs.Length > 0 && nextParagraphs.Length > 0)
            {
                Assert.That(currentParagraphs[^1], Is.Not.EqualTo(nextParagraphs[0]),
                    "With zero overlap, chunks should not share content");
            }
        }
    }

    [Test]
    public void Constructor_DefaultParameters_UsesRecommendedValues()
    {
        // Arrange & Act
        var sut = new SemanticChunker(); // Use defaults
        var text = new string('a', 500) + "\n\n" + new string('b', 500) + "\n\n" + new string('c', 500);

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.GreaterThan(1),
            "Default 800-char max size should split this 1500+ char text");
    }
}
