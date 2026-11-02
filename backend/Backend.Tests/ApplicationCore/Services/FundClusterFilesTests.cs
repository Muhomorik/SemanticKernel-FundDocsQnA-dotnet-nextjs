using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.ApplicationCore.Services;
using Backend.API.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;

namespace Backend.Tests.ApplicationCore.Services;

[TestFixture]
[Category("Unit")]
[Category("FundClusters")]
public class FundClusterFilesTests
{
    private const string Manifest =
        """{"week":"2026-W44","default":"cosine","methods":[{"id":"cosine","name":"Cosine","file":"cosine.json"},{"id":"aitchison","name":"Aitchison","file":"aitchison.json"}]}""";

    private IFixture _fixture = null!;
    private string _contentRoot = null!;
    private string _dataDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "fund-clusters-tests-" + Guid.NewGuid().ToString("N"));
        _dataDirectory = Path.Combine(_contentRoot, "Data", "fund-clusters");
        Directory.CreateDirectory(_dataDirectory);

        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _fixture.Inject(new BackendOptions
        {
            EmbeddingsFilePath = "test.json",
            OpenAIApiKey = "test-key",
            OpenAIEmbeddingModel = "text-embedding-3-small",
            MemoryCollectionName = "test",
            FundClustersDirectory = "Data/fund-clusters"
        });
        _fixture.Freeze<Mock<IHostEnvironment>>()
            .Setup(x => x.ContentRootPath)
            .Returns(_contentRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_contentRoot))
            Directory.Delete(_contentRoot, recursive: true);
    }

    private void WriteFile(string name, string content = "{}") =>
        File.WriteAllText(Path.Combine(_dataDirectory, name), content);

    // ─── GetManifestPath ──────────────────────────────────────────────────────

    [Test]
    public void GetManifestPath_FileExists_ReturnsFullPath()
    {
        // Arrange
        WriteFile("methods.json", Manifest);
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetManifestPath();

        // Assert
        Assert.That(result, Is.EqualTo(Path.Combine(_dataDirectory, "methods.json")));
    }

    [Test]
    public void GetManifestPath_FileMissing_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetManifestPath();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetManifestPath_DirectoryMissing_ReturnsNull()
    {
        // Arrange
        Directory.Delete(_dataDirectory, recursive: true);
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetManifestPath();

        // Assert
        Assert.That(result, Is.Null);
    }

    // ─── GetFundsPath ─────────────────────────────────────────────────────────

    [Test]
    public void GetFundsPath_FileExists_ReturnsFullPath()
    {
        // Arrange
        WriteFile("funds.json");
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetFundsPath();

        // Assert
        Assert.That(result, Is.EqualTo(Path.Combine(_dataDirectory, "funds.json")));
    }

    [Test]
    public void GetFundsPath_FileMissing_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetFundsPath();

        // Assert
        Assert.That(result, Is.Null);
    }

    // ─── GetMethodPath ────────────────────────────────────────────────────────

    [Test]
    public void GetMethodPath_IdInManifest_ReturnsFileFromManifest()
    {
        // Arrange
        WriteFile("methods.json", Manifest);
        WriteFile("aitchison.json");
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetMethodPath("aitchison");

        // Assert
        Assert.That(result, Is.EqualTo(Path.Combine(_dataDirectory, "aitchison.json")));
    }

    [Test]
    public void GetMethodPath_IdNotInManifest_ReturnsNull()
    {
        // Arrange
        WriteFile("methods.json", Manifest);
        WriteFile("hellinger.json");
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetMethodPath("hellinger");

        // Assert
        Assert.That(result, Is.Null);
    }

    [TestCase("../methods")]
    [TestCase("..\\methods")]
    [TestCase("funds")]
    [TestCase("methods")]
    [TestCase("")]
    public void GetMethodPath_IdNotAMethod_ReturnsNull(string methodId)
    {
        // Arrange
        WriteFile("methods.json", Manifest);
        WriteFile("funds.json");
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetMethodPath(methodId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetMethodPath_ManifestFileEscapesDirectory_ReturnsNull()
    {
        // Arrange
        WriteFile("methods.json",
            """{"default":"evil","methods":[{"id":"evil","name":"Evil","file":"../secret.json"}]}""");
        File.WriteAllText(Path.Combine(_contentRoot, "Data", "secret.json"), "{}");
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetMethodPath("evil");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetMethodPath_MethodFileMissing_ReturnsNull()
    {
        // Arrange
        WriteFile("methods.json", Manifest);
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetMethodPath("cosine");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetMethodPath_ManifestMissing_ReturnsNull()
    {
        // Arrange
        WriteFile("cosine.json");
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetMethodPath("cosine");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetMethodPath_ManifestInvalidJson_ReturnsNull()
    {
        // Arrange
        WriteFile("methods.json", "not json");
        WriteFile("cosine.json");
        var sut = _fixture.Create<FundClusterFiles>();

        // Act
        var result = sut.GetMethodPath("cosine");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetMethodPath_ManifestReplacedAfterFirstCall_UsesNewManifest()
    {
        // Arrange
        WriteFile("methods.json", Manifest);
        WriteFile("cosine.json");
        WriteFile("hellinger.json");
        var sut = _fixture.Create<FundClusterFiles>();
        sut.GetMethodPath("cosine");

        WriteFile("methods.json",
            """{"default":"hellinger","methods":[{"id":"hellinger","name":"Hellinger","file":"hellinger.json"}]}""");
        File.SetLastWriteTimeUtc(Path.Combine(_dataDirectory, "methods.json"), DateTime.UtcNow.AddMinutes(1));

        // Act
        var result = sut.GetMethodPath("hellinger");

        // Assert
        Assert.That(result, Is.EqualTo(Path.Combine(_dataDirectory, "hellinger.json")));
    }
}
