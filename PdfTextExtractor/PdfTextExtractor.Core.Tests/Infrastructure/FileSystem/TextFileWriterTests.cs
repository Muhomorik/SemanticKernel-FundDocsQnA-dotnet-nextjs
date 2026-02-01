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
    public async Task WritePagesAsync_ValidPages_CreatesMultipleFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var pdfFileName = "test.pdf";
        var pages = new[]
        {
            new DocumentPage
            {
                SourceFile = "test.pdf",
                PageNumber = 1,
                PageText = "First page content"
            },
            new DocumentPage
            {
                SourceFile = "test.pdf",
                PageNumber = 2,
                PageText = "Second page content"
            }
        };

        try
        {
            // Act
            await _sut.WritePagesAsync(tempDir, pdfFileName, pages);

            // Assert
            var page1File = Path.Combine(tempDir, "test_page_1.txt");
            var page2File = Path.Combine(tempDir, "test_page_2.txt");

            Assert.That(File.Exists(page1File), Is.True);
            Assert.That(File.Exists(page2File), Is.True);

            var page1Content = await File.ReadAllTextAsync(page1File);
            var page2Content = await File.ReadAllTextAsync(page2File);

            Assert.That(page1Content, Is.EqualTo("First page content"));
            Assert.That(page2Content, Is.EqualTo("Second page content"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task WritePagesAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var pages = _fixture.CreateMany<DocumentPage>().ToArray();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await _sut.WritePagesAsync(tempDir, "test.pdf", pages, cts.Token));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
