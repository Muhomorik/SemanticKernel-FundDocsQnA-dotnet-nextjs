using Preprocessor.Models;
using Preprocessor.Services;

namespace Preprocessor.Tests.Services;

[TestFixture]
[TestOf(typeof(ChunkSanitizer))]
public class ChunkSanitizerTests
{
    private ChunkSanitizer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ChunkSanitizer();
    }

    private static readonly object[] SanitizeTestCases =
    [
        new object[] { "This is some text 1 2 3 4 5 6 7 and more text", "This is some text  and more text" },
        new object[] { "1 2 3 4 5 6 7 This is some text", "This is some text" },
        new object[] { "This is some text 1 2 3 4 5 6 7", "This is some text" },
        new object[] { "1 2 3 4 5 6 7 Start 1 2 3 4 5 6 7 Middle 1 2 3 4 5 6 7 End", "Start  Middle  End" },
        new object[] { "This is clean text without noise patterns", "This is clean text without noise patterns" }
    ];

    [Test]
    [TestCaseSource(nameof(SanitizeTestCases))]
    public void Sanitize_WithVariousInputs_SanitizesCorrectly(string input, string expected)
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new()
            {
                SourceFile = "test.pdf",
                PageNumber = 1,
                ChunkIndex = 0,
                Content = input
            }
        };

        // Act
        var result = _sut.Sanitize(chunks).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Is.EqualTo(expected));
    }

    [Test]
    public void Sanitize_WithMultipleChunks_SanitizesAll()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new()
            {
                SourceFile = "test.pdf",
                PageNumber = 1,
                ChunkIndex = 0,
                Content = "Chunk 1 with 1 2 3 4 5 6 7 noise"
            },
            new()
            {
                SourceFile = "test.pdf",
                PageNumber = 1,
                ChunkIndex = 1,
                Content = "Clean chunk 2"
            },
            new()
            {
                SourceFile = "test.pdf",
                PageNumber = 2,
                ChunkIndex = 0,
                Content = "1 2 3 4 5 6 7 Chunk 3"
            }
        };

        // Act
        var result = _sut.Sanitize(chunks).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].Content, Is.EqualTo("Chunk 1 with  noise"));
        Assert.That(result[1].Content, Is.EqualTo("Clean chunk 2"));
        Assert.That(result[2].Content, Is.EqualTo("Chunk 3"));
    }

    [Test]
    public void Sanitize_WithEmptyCollection_ReturnsEmptyCollection()
    {
        // Arrange
        var chunks = new List<DocumentChunk>();

        // Act
        var result = _sut.Sanitize(chunks).ToList();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Sanitize_PreservesOtherChunkProperties()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new()
            {
                SourceFile = "test.pdf",
                PageNumber = 5,
                ChunkIndex = 3,
                Content = "Text 1 2 3 4 5 6 7 here"
            }
        };

        // Act
        var result = _sut.Sanitize(chunks).ToList();

        // Assert
        Assert.That(result[0].SourceFile, Is.EqualTo("test.pdf"));
        Assert.That(result[0].PageNumber, Is.EqualTo(5));
        Assert.That(result[0].ChunkIndex, Is.EqualTo(3));
    }
}
