using Microsoft.Extensions.Logging;

using Moq;

using Preprocessor.Extractors;
using Preprocessor.Models;
using Preprocessor.Services;

namespace Preprocessor.Tests.Services;

[TestFixture]
public class PreprocessorServiceTests
{
    private Mock<IPdfExtractor> _extractorMock = null!;
    private Mock<IEmbeddingService> _embeddingServiceMock = null!;
    private Mock<ILogger<PreprocessorService>> _loggerMock = null!;
    private PreprocessorService _service = null!;
    private string _tempDir = null!;

    [SetUp]
    public void Setup()
    {
        _extractorMock = new Mock<IPdfExtractor>();
        _extractorMock.Setup(x => x.MethodName).Returns("pdfpig");

        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _loggerMock = new Mock<ILogger<PreprocessorService>>();

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
        var result = await _service.ProcessAsync(options);

        // Assert
        Assert.That(result, Is.Not.EqualTo(0));
    }

    [Test]
    public async Task ProcessAsync_WithNonExistentInputDir_ShouldReturnNonZeroExitCode()
    {
        // Arrange
        var options = CreateOptions(input: Path.Combine(_tempDir, "nonexistent"));

        // Act
        var result = await _service.ProcessAsync(options);

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
        var result = await _service.ProcessAsync(options);

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task ProcessAsync_WithValidOptions_ShouldProcessPdfs()
    {
        // Arrange
        var inputDir = Path.Combine(_tempDir, "input");
        var outputDir = Path.Combine(_tempDir, "output");
        var outputPath = Path.Combine(outputDir, "embeddings.json");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

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

        var options = CreateOptions(input: inputDir, output: outputPath);

        // Act
        var result = await _service.ProcessAsync(options);

        // Assert
        Assert.That(result, Is.EqualTo(0));
        Assert.That(File.Exists(outputPath), Is.True);
    }

    [Test]
    public async Task ProcessAsync_WithAppendOption_ShouldAppendToExistingFile()
    {
        // Arrange
        var inputDir = Path.Combine(_tempDir, "input");
        var outputPath = Path.Combine(_tempDir, "embeddings.json");
        Directory.CreateDirectory(inputDir);

        // Create existing embeddings file
        var existingEmbeddings = """
                                 [{"id":"existing","text":"Existing text","embedding":[0.1,0.2],"source":"old.pdf","page":1}]
                                 """;
        await File.WriteAllTextAsync(outputPath, existingEmbeddings);

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

        var options = CreateOptions(input: inputDir, output: outputPath, append: true);

        // Act
        var result = await _service.ProcessAsync(options);

        // Assert
        Assert.That(result, Is.EqualTo(0));

        var outputContent = await File.ReadAllTextAsync(outputPath);
        Assert.That(outputContent, Does.Contain("existing"));
        Assert.That(outputContent, Does.Contain("new_page1_chunk0"));
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
        var result = await _service.ProcessAsync(options, cts.Token);

        // Assert
        Assert.That(result, Is.Not.EqualTo(0));
    }

    private CliOptions CreateOptions(
        string method = "pdfpig",
        string? input = null,
        string? output = null,
        bool append = false)
    {
        return new CliOptions
        {
            Method = method,
            Input = input ?? _tempDir,
            Output = output ?? Path.Combine(_tempDir, "output.json"),
            Append = append,
            VisionModel = "llava",
            EmbeddingModel = "nomic-embed-text",
            OllamaUrl = "http://localhost:11434"
        };
    }
}