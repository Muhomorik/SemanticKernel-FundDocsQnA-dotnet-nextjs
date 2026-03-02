using System.Reactive.Subjects;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(DualWriteChartIngestionService))]
public class DualWriteChartIngestionService_IngestChartDataAsyncTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private Mock<IAboutFundChartIngestionService> _innerMock = null!;
    private Mock<IFundSyncApiClient> _apiClientMock = null!;
    private Mock<IFundProfileRepository> _profileRepoMock = null!;
    private Subject<BackendSyncStatus> _syncSubject = null!;
    private DualWriteChartIngestionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _innerMock = _fixture.Freeze<Mock<IAboutFundChartIngestionService>>();
        _apiClientMock = _fixture.Freeze<Mock<IFundSyncApiClient>>();
        _profileRepoMock = _fixture.Freeze<Mock<IFundProfileRepository>>();

        _syncSubject = new Subject<BackendSyncStatus>();
        _fixture.Inject(_syncSubject);

        _sut = _fixture.Create<DualWriteChartIngestionService>();
    }

    [Test]
    public async Task IngestChartDataAsync_InnerSucceeds_BackendSucceeds_ReturnsInnerCount()
    {
        // Arrange
        var isinId = _fixture.Create<IsinId>();
        var pageData = CreatePageDataWithNoSlots();
        var profile = CreateProfile(isinId);

        _innerMock.Setup(x => x.IngestChartDataAsync(pageData, isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        _profileRepoMock.Setup(x => x.GetByIsinAsync(isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _apiClientMock.Setup(x => x.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", HistoryRecordsInserted = 10 });

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert
        Assert.That(result, Is.EqualTo(10));
        _innerMock.Verify(x => x.IngestChartDataAsync(pageData, isinId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IngestChartDataAsync_InnerSucceeds_BackendThrows_ReturnsInnerCount()
    {
        // Arrange
        var isinId = _fixture.Create<IsinId>();
        var pageData = CreatePageDataWithNoSlots();
        var profile = CreateProfile(isinId);

        _innerMock.Setup(x => x.IngestChartDataAsync(pageData, isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _profileRepoMock.Setup(x => x.GetByIsinAsync(isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _apiClientMock.Setup(x => x.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert — SQLite result returned despite API failure
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public async Task IngestChartDataAsync_ProfileNotFound_SkipsBackendCall()
    {
        // Arrange
        var isinId = _fixture.Create<IsinId>();
        var pageData = CreatePageDataWithNoSlots();

        _innerMock.Setup(x => x.IngestChartDataAsync(pageData, isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _profileRepoMock.Setup(x => x.GetByIsinAsync(isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FundProfile?)null);

        BackendSyncStatus? received = null;
        _syncSubject.Subscribe(s => received = s);

        // Act
        await _sut.IngestChartDataAsync(pageData, isinId);
        await Task.Delay(200);

        // Assert
        _apiClientMock.Verify(
            x => x.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.IsSuccess, Is.False);
        Assert.That(received.Message, Does.Contain("not found"));
    }

    [Test]
    public async Task IngestChartDataAsync_BackendError_PublishesSyncError()
    {
        // Arrange
        var isinId = _fixture.Create<IsinId>();
        var pageData = CreatePageDataWithNoSlots();
        var profile = CreateProfile(isinId);

        _innerMock.Setup(x => x.IngestChartDataAsync(pageData, isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        _profileRepoMock.Setup(x => x.GetByIsinAsync(isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _apiClientMock.Setup(x => x.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Server error"));

        BackendSyncStatus? received = null;
        _syncSubject.Subscribe(s => received = s);

        // Act
        await _sut.IngestChartDataAsync(pageData, isinId);
        await Task.Delay(200);

        // Assert
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.IsSuccess, Is.False);
        Assert.That(received.Message, Does.Contain("Server error"));
    }

    private AboutFundPageData CreatePageDataWithNoSlots()
    {
        return new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>()
        };
    }

    private FundProfile CreateProfile(IsinId isinId)
    {
        return new FundProfile
        {
            Id = isinId,
            Name = $"Test Fund {isinId.Isin}",
            FirstSeenAt = DateTimeOffset.UtcNow
        };
    }
}
