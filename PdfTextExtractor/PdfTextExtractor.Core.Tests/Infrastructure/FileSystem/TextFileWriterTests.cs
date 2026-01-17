using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Infrastructure.FileSystem;
using PdfTextExtractor.Core.Models;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Infrastructure.FileSystem;

[TestFixture]
[TestOf(typeof(TextFileWriter))]
public class TextFileWriterTests
{
    private IFixture _fixture;
    private TextFileWriter _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _sut = _fixture.Create<TextFileWriter>();
    }

    [Test]
    public async Task WriteTextFileAsync_ValidContent_WritesFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        var content = "Test content";

        try
        {
            // Act
            await _sut.WriteTextFileAsync(tempFile, content);

            // Assert
            Assert.That(File.Exists(tempFile), Is.True);
            var writtenContent = await File.ReadAllTextAsync(tempFile);
            Assert.That(writtenContent, Is.EqualTo(content));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task WriteChunksAsync_ValidChunks_FormatsCorrectly()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        var chunks = new[]
        {
            new DocumentChunk
            {
                SourceFile = "test.pdf",
                PageNumber = 1,
                ChunkIndex = 0,
                Content = "First chunk"
            },
            new DocumentChunk
            {
                SourceFile = "test.pdf",
                PageNumber = 1,
                ChunkIndex = 1,
                Content = "Second chunk"
            }
        };

        try
        {
            // Act
            await _sut.WriteChunksAsync(tempFile, chunks);

            // Assert
            Assert.That(File.Exists(tempFile), Is.True);
            var writtenContent = await File.ReadAllTextAsync(tempFile);
            Assert.That(writtenContent, Does.Contain("First chunk"));
            Assert.That(writtenContent, Does.Contain("Second chunk"));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task WriteChunksAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        var chunks = _fixture.CreateMany<DocumentChunk>().ToArray();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _sut.WriteChunksAsync(tempFile, chunks, cts.Token));
    }
}
