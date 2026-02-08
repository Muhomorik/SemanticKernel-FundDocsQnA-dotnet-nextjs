using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Infrastructure.FileSystem;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Infrastructure.FileSystem;

[TestFixture]
[TestOf(typeof(FileSystemService))]
public class FileSystemServiceTests
{
    private IFixture _fixture;
    private FileSystemService _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _sut = _fixture.Create<FileSystemService>();
    }

    [Test]
    public void GetPdfFiles_ExistingFolderWithPdfs_ReturnsFilePaths()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);

        try
        {
            var file1 = Path.Combine(tempFolder, "doc1.pdf");
            var file2 = Path.Combine(tempFolder, "doc2.pdf");
            File.WriteAllText(file1, "test");
            File.WriteAllText(file2, "test");

            // Act
            var result = _sut.GetPdfFiles(tempFolder);

            // Assert
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result, Does.Contain(file1));
            Assert.That(result, Does.Contain(file2));
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
        }
    }

    [Test]
    public void GetTextFiles_NonExistingFolder_ReturnsEmptyArray()
    {
        // Arrange
        var nonExistingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = _sut.GetTextFiles(nonExistingFolder);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void EnsureDirectoryExists_NonExistingDirectory_CreatesDirectory()
    {
        // Arrange
        var newFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Act
            _sut.EnsureDirectoryExists(newFolder);

            // Assert
            Assert.That(Directory.Exists(newFolder), Is.True);
        }
        finally
        {
            if (Directory.Exists(newFolder))
                Directory.Delete(newFolder);
        }
    }
}
