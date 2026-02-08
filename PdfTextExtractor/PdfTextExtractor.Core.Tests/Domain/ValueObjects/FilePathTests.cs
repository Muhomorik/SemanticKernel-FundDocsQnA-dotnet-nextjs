using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.ValueObjects;

[TestFixture]
[TestOf(typeof(FilePath))]
public class FilePathTests
{
    private IFixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());
    }

    [Test]
    public void Create_ValidPath_ReturnsFilePath()
    {
        // Arrange
        var validPath = Path.Combine(Path.GetTempPath(), "test.pdf");

        // Act
        var result = FilePath.Create(validPath);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.Not.Empty);
    }

    [Test]
    public void Create_ValidPath_StoresFullPath()
    {
        // Arrange
        var relativePath = "test.pdf";

        // Act
        var result = FilePath.Create(relativePath);

        // Assert
        Assert.That(Path.IsPathRooted(result.Value), Is.True);
    }

    [Test]
    public void FileName_ValidFilePath_ReturnsFileNameOnly()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "document.pdf");
        var filePath = FilePath.Create(path);

        // Act
        var fileName = filePath.FileName;

        // Assert
        Assert.That(fileName, Is.EqualTo("document.pdf"));
    }

    [Test]
    public void Directory_ValidFilePath_ReturnsDirectoryPath()
    {
        // Arrange
        var expectedDirectory = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var path = Path.Combine(expectedDirectory, "document.pdf");
        var filePath = FilePath.Create(path);

        // Act
        var directory = filePath.Directory;

        // Assert
        Assert.That(directory, Does.Contain(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)));
    }

    [Test]
    public void Extension_ValidFilePath_ReturnsExtension()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "document.pdf");
        var filePath = FilePath.Create(path);

        // Act
        var extension = filePath.Extension;

        // Assert
        Assert.That(extension, Is.EqualTo(".pdf"));
    }
}
