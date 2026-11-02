using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.ApplicationCore.Services;
using Backend.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Backend.Tests.Controllers;

[TestFixture]
[Category("Unit")]
[Category("Controller")]
[Category("FundClusters")]
public class FundClustersControllerTests
{
    private IFixture _fixture = null!;
    private Mock<IFundClusterFiles> _filesMock = null!;
    private FundClustersController _sut = null!;
    private string _file = null!;

    [SetUp]
    public void SetUp()
    {
        _file = Path.GetTempFileName();

        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _filesMock = _fixture.Freeze<Mock<IFundClusterFiles>>();
        _sut = _fixture.Build<FundClustersController>().OmitAutoProperties().Create();
    }

    [TearDown]
    public void TearDown() => File.Delete(_file);

    private static void AssertJsonFile(IActionResult result, string expectedPath)
    {
        Assert.That(result, Is.InstanceOf<PhysicalFileResult>());
        var file = (PhysicalFileResult)result;
        Assert.Multiple(() =>
        {
            Assert.That(file.FileName, Is.EqualTo(expectedPath));
            Assert.That(file.ContentType, Is.EqualTo("application/json"));
            Assert.That(file.LastModified, Is.Not.Null);
            Assert.That(file.EntityTag, Is.Not.Null);
        });
    }

    private static void AssertStatus(IActionResult result, int statusCode) =>
        Assert.That(result, Is.InstanceOf<ObjectResult>()
            .With.Property("StatusCode").EqualTo(statusCode));

    // ─── GetMethods ───────────────────────────────────────────────────────────

    [Test]
    public void GetMethods_ManifestExists_ReturnsJsonFile()
    {
        // Arrange
        _filesMock.Setup(x => x.GetManifestPath()).Returns(_file);

        // Act
        var result = _sut.GetMethods();

        // Assert
        AssertJsonFile(result, _file);
    }

    [Test]
    public void GetMethods_ManifestMissing_Returns503()
    {
        // Arrange
        _filesMock.Setup(x => x.GetManifestPath()).Returns((string?)null);

        // Act
        var result = _sut.GetMethods();

        // Assert
        AssertStatus(result, StatusCodes.Status503ServiceUnavailable);
    }

    // ─── GetFunds ─────────────────────────────────────────────────────────────

    [Test]
    public void GetFunds_FundsExist_ReturnsJsonFile()
    {
        // Arrange
        _filesMock.Setup(x => x.GetFundsPath()).Returns(_file);

        // Act
        var result = _sut.GetFunds();

        // Assert
        AssertJsonFile(result, _file);
    }

    [Test]
    public void GetFunds_FundsMissing_Returns503()
    {
        // Arrange
        _filesMock.Setup(x => x.GetFundsPath()).Returns((string?)null);

        // Act
        var result = _sut.GetFunds();

        // Assert
        AssertStatus(result, StatusCodes.Status503ServiceUnavailable);
    }

    // ─── GetMethod ────────────────────────────────────────────────────────────

    [Test]
    public void GetMethod_KnownId_ReturnsJsonFile()
    {
        // Arrange
        _filesMock.Setup(x => x.GetManifestPath()).Returns(_file);
        _filesMock.Setup(x => x.GetMethodPath("cosine")).Returns(_file);

        // Act
        var result = _sut.GetMethod("cosine");

        // Assert
        AssertJsonFile(result, _file);
    }

    [Test]
    public void GetMethod_UnknownId_Returns404()
    {
        // Arrange
        _filesMock.Setup(x => x.GetManifestPath()).Returns(_file);
        _filesMock.Setup(x => x.GetMethodPath(It.IsAny<string>())).Returns((string?)null);

        // Act
        var result = _sut.GetMethod("unknown");

        // Assert
        AssertStatus(result, StatusCodes.Status404NotFound);
    }

    [Test]
    public void GetMethod_ManifestMissing_Returns503()
    {
        // Arrange
        _filesMock.Setup(x => x.GetManifestPath()).Returns((string?)null);

        // Act
        var result = _sut.GetMethod("cosine");

        // Assert
        AssertStatus(result, StatusCodes.Status503ServiceUnavailable);
    }

    [Test]
    public void GetMethods_SameFileTwice_ReturnsSameEntityTag()
    {
        // Arrange
        _filesMock.Setup(x => x.GetManifestPath()).Returns(_file);

        // Act
        var first = (PhysicalFileResult)_sut.GetMethods();
        var second = (PhysicalFileResult)_sut.GetMethods();

        // Assert
        Assert.That(second.EntityTag, Is.EqualTo(first.EntityTag));
    }
}
