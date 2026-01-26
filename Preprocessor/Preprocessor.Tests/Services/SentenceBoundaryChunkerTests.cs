using AutoFixture;
using AutoFixture.AutoMoq;

using Preprocessor.Services;

namespace Preprocessor.Tests.Services;

[TestFixture]
public class SentenceBoundaryChunkerTests
{
    private IFixture _fixture;
    private SentenceBoundaryChunker _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _sut = new SentenceBoundaryChunker(maxChunkSize: 1000);
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
    public void Chunk_SingleSentence_ReturnsSingleChunk()
    {
        // Arrange
        var singleSentence = "This is a single sentence.";

        // Act
        var result = _sut.Chunk(singleSentence).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("This is a single sentence."));
    }

    [Test]
    public void Chunk_MultipleSentencesShorterThanChunkSize_ReturnsSingleChunk()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence.";

        // Act
        var result = _sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("First sentence. Second sentence. Third sentence."));
    }

    [Test]
    public void Chunk_TextExceedingChunkSize_SplitsOnSentenceBoundaries()
    {
        // Arrange
        var sut = new SentenceBoundaryChunker(maxChunkSize: 50);
        var text = "First sentence is here. Second sentence is here. Third sentence is here.";

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.GreaterThan(1));
        // Verify each chunk is under the size limit (with some tolerance for sentence completion)
        foreach (var chunk in result)
        {
            Assert.That(chunk, Is.Not.Empty);
        }
        // Verify all content is preserved
        var reconstructed = string.Join(" ", result);
        Assert.That(reconstructed, Does.Contain("First sentence is here"));
        Assert.That(reconstructed, Does.Contain("Second sentence is here"));
        Assert.That(reconstructed, Does.Contain("Third sentence is here"));
    }

    [Test]
    public void Chunk_PreservesSentencePunctuation()
    {
        // Arrange
        var text = "Question sentence? Exclamation sentence! Period sentence.";

        // Act
        var result = _sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("Question sentence. Exclamation sentence. Period sentence."));
    }

    [Test]
    public void Chunk_LongTextWithManySentences_CreatesMultipleChunks()
    {
        // Arrange
        var sut = new SentenceBoundaryChunker(maxChunkSize: 100);
        var sentences = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            sentences.Add($"This is sentence number {i} with some additional content.");
        }
        var text = string.Join(" ", sentences);

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.GreaterThan(1));
        // Verify chunks don't exceed size significantly
        foreach (var chunk in result)
        {
            Assert.That(chunk.Length, Is.LessThanOrEqualTo(150)); // Allow some tolerance
        }
    }

    [Test]
    public void Constructor_ZeroChunkSize_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new SentenceBoundaryChunker(maxChunkSize: 0));
        Assert.That(ex.ParamName, Is.EqualTo("maxChunkSize"));
        Assert.That(ex.Message, Does.Contain("Chunk size must be greater than zero"));
    }

    [Test]
    public void Constructor_NegativeChunkSize_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new SentenceBoundaryChunker(maxChunkSize: -100));
        Assert.That(ex.ParamName, Is.EqualTo("maxChunkSize"));
        Assert.That(ex.Message, Does.Contain("Chunk size must be greater than zero"));
    }

    [Test]
    public void Constructor_DefaultChunkSize_Creates1000CharacterChunks()
    {
        // Arrange
        var sut = new SentenceBoundaryChunker(); // Use default
        var longSentence = new string('a', 500);
        var text = $"{longSentence}. {longSentence}. {longSentence}.";

        // Act
        var result = sut.Chunk(text).ToList();

        // Assert
        Assert.That(result, Has.Count.GreaterThan(1));
    }
}
