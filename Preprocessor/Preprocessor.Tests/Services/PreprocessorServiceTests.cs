using Microsoft.Extensions.Logging;

using Moq;

using Preprocessor.Extractors;
using Preprocessor.Models;
using Preprocessor.Outputs;
using Preprocessor.Services;

namespace Preprocessor.Tests.Services;

[TestFixture]
[TestOf(typeof(PreprocessorService))]
public class PreprocessorServiceTests
{
    private Mock<IPdfExtractor> _extractorMock = null!;
    private Mock<IEmbeddingService> _embeddingServiceMock = null!;
    private Mock<ILogger<PreprocessorService>> _loggerMock = null!;
    private Mock<IEmbeddingOutput> _outputMock = null!;
    private PreprocessorService _service = null!;
    private string _tempDir = null!;

    [SetUp]
    public void Setup()
    {
        _extractorMock = new Mock<IPdfExtractor>();
        _extractorMock.Setup(x => x.MethodName).Returns("pdfpig");

        _embeddingServiceMock = new Mock<IEmbeddingService>();

        _loggerMock = new Mock<ILogger<PreprocessorService>>();

        _outputMock = new Mock<IEmbeddingOutput>();
        _outputMock.Setup(x => x.DisplayName).Returns("Mock Output");
        _outputMock.Setup(x => x.LoadExistingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmbeddingResult>().AsReadOnly());

        _service = new PreprocessorService(
            new[] { _extractorMock.Object },
            _embeddingServiceMock.Object,
            _loggerMock.Object);

        _tempDir = Path.Combine(Path.GetTempPath(), $"PreprocessorTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    public async Task ProcessAsync_WithInvalidMethod_ShouldReturnNonZeroExitCode()
    {
        // Arrange
        var options = CreateOptions("invalid-method");

        // Act
        var result = await _service.ProcessAsync(options, _outputMock.Object);

        // Assert
        Assert.That(result, Is.Not.EqualTo(0));
    }

    [Test]
    public async Task ProcessAsync_WithNonExistentInputDir_ShouldReturnNonZeroExitCode()
    {
        // Arrange
        var options = CreateOptions(input: Path.Combine(_tempDir, "nonexistent"));

        // Act
        var result = await _service.ProcessAsync(options, _outputMock.Object);

        // Assert
        Assert.That(result, Is.Not.EqualTo(0));
    }

    [Test]
    public async Task ProcessAsync_WithNoPdfFiles_ShouldReturnZeroExitCode()
    {
        // Arrange
        var inputDir = Path.Combine(_tempDir, "input");
        Directory.CreateDirectory(inputDir);

        var options = CreateOptions(input: inputDir);

        // Act
        var result = await _service.ProcessAsync(options, _outputMock.Object);

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task ProcessAsync_WithValidOptions_ShouldProcessPdfs()
    {
        // Arrange
        var inputDir = Path.Combine(_tempDir, "input");
        Directory.CreateDirectory(inputDir);

        // Create a dummy PDF file (just to have something in the directory)
        var dummyPdf = Path.Combine(inputDir, "test.pdf");
        await File.WriteAllBytesAsync(dummyPdf, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF magic bytes

        var chunks = new List<DocumentChunk>
        {
            new() { SourceFile = "test.pdf", PageNumber = 1, ChunkIndex = 0, Content = "Test content" }
        };

        _extractorMock
            .Setup(x => x.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        var options = CreateOptions(input: inputDir);

        // Act
        var result = await _service.ProcessAsync(options, _outputMock.Object);

        // Assert
        Assert.That(result, Is.EqualTo(0));
        _outputMock.Verify(x => x.SaveAsync(It.IsAny<IReadOnlyList<EmbeddingResult>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithExistingEmbeddings_ShouldIncludeThemInOutput()
    {
        // Arrange
        var inputDir = Path.Combine(_tempDir, "input");
        Directory.CreateDirectory(inputDir);

        // Set up existing embeddings to be returned by LoadExistingAsync
        var existingEmbeddings = new List<EmbeddingResult>
        {
            new()
            {
                Id = "existing",
                Text = "Existing text",
                Embedding = new float[] { 0.1f, 0.2f },
                Source = "old.pdf",
                Page = 1
            }
        }.AsReadOnly();

        _outputMock
            .Setup(x => x.LoadExistingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmbeddings);

        // Create a dummy PDF file
        var dummyPdf = Path.Combine(inputDir, "new.pdf");
        await File.WriteAllBytesAsync(dummyPdf, new byte[] { 0x25, 0x50, 0x44, 0x46 });

        var chunks = new List<DocumentChunk>
        {
            new() { SourceFile = "new.pdf", PageNumber = 1, ChunkIndex = 0, Content = "New content" }
        };

        _extractorMock
            .Setup(x => x.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.3f, 0.4f });

        var options = CreateOptions(input: inputDir);

        // Act
        var result = await _service.ProcessAsync(options, _outputMock.Object);

        // Assert
        Assert.That(result, Is.EqualTo(0));
        _outputMock.Verify(
            x => x.SaveAsync(
                It.Is<IReadOnlyList<EmbeddingResult>>(list => list.Count == 2 && list.Any(e => e.Id == "existing")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WithCancellation_ShouldReturnNonZeroExitCode()
    {
        // Arrange
        var inputDir = Path.Combine(_tempDir, "input");
        Directory.CreateDirectory(inputDir);

        var dummyPdf = Path.Combine(inputDir, "test.pdf");
        await File.WriteAllBytesAsync(dummyPdf, new byte[] { 0x25, 0x50, 0x44, 0x46 });

        _extractorMock
            .Setup(x => x.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var options = CreateOptions(input: inputDir);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.ProcessAsync(options, _outputMock.Object, cts.Token);

        // Assert
        Assert.That(result, Is.Not.EqualTo(0));
    }

    private ProcessingOptions CreateOptions(
        string? input = null)
    {
        return new ProcessingOptions
        {
            InputDirectory = input ?? _tempDir
        };
    }
}