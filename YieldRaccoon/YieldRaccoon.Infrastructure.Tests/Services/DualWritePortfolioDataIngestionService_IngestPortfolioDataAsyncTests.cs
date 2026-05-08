using System.Reactive.Subjects;
using Moq;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(DualWritePortfolioDataIngestionService))]
public class DualWritePortfolioDataIngestionService_IngestPortfolioDataAsyncTests
{
    private const string SamplePayload = """
        {
          "countryChartData": [{ "name": "USA", "y": 50.0, "countryCode": "US" }],
          "sectorChartData": [{ "name": "Teknik", "y": 50.0 }]
        }
        """;

    private Mock<ILogger> _loggerMock = null!;
    private Mock<IPortfolioDataIngestionService> _innerMock = null!;
    private Mock<IFundSyncApiClient> _apiClientMock = null!;
    private Subject<BackendSyncStatus> _syncSubject = null!;
    private DualWritePortfolioDataIngestionService _sut = null!;
    private IsinId _isinId;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger>();
        _innerMock = new Mock<IPortfolioDataIngestionService>();
        _apiClientMock = new Mock<IFundSyncApiClient>();
        _syncSubject = new Subject<BackendSyncStatus>();
        _isinId = IsinId.Create("SE0008613939");

        _sut = new DualWritePortfolioDataIngestionService(
            _loggerMock.Object,
            _innerMock.Object,
            _apiClientMock.Object,
            _syncSubject);
    }

    private static AboutFundPageData PageDataWithJson(string? json) => new()
    {
        OrderBookId = OrderBookId.Create("950780"),
        PortfolioDataJson = json
    };

    [Test]
    public async Task IngestPortfolioDataAsync_LocalSucceeds_ReturnsLocalCount()
    {
        _innerMock.Setup(x => x.IngestPortfolioDataAsync(
                It.IsAny<AboutFundPageData>(), _isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        _apiClientMock.Setup(x => x.SyncPortfolioAllocationsAsync(
                It.IsAny<FundPortfolioAllocationsSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK" });

        var result = await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);

        Assert.That(result, Is.EqualTo(4));
        _innerMock.Verify(x => x.IngestPortfolioDataAsync(
            It.IsAny<AboutFundPageData>(), _isinId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IngestPortfolioDataAsync_LocalSucceedsCloudFails_StillReturnsLocalCount()
    {
        _innerMock.Setup(x => x.IngestPortfolioDataAsync(
                It.IsAny<AboutFundPageData>(), _isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        _apiClientMock.Setup(x => x.SyncPortfolioAllocationsAsync(
                It.IsAny<FundPortfolioAllocationsSyncRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);

        // Cloud failure must not propagate — local result is what matters
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void IngestPortfolioDataAsync_LocalThrows_PropagatesAndSkipsCloud()
    {
        _innerMock.Setup(x => x.IngestPortfolioDataAsync(
                It.IsAny<AboutFundPageData>(), _isinId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB went away"));

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId));

        _apiClientMock.Verify(x => x.SyncPortfolioAllocationsAsync(
            It.IsAny<FundPortfolioAllocationsSyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Cloud sync must not run when local persistence failed");
    }

    [Test]
    public async Task IngestPortfolioDataAsync_NoPortfolioJson_SkipsCloudSync()
    {
        _innerMock.Setup(x => x.IngestPortfolioDataAsync(
                It.IsAny<AboutFundPageData>(), _isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.IngestPortfolioDataAsync(PageDataWithJson(null), _isinId);

        // Give any background task a chance to run, then verify no cloud call happened
        await Task.Delay(50);
        _apiClientMock.Verify(x => x.SyncPortfolioAllocationsAsync(
            It.IsAny<FundPortfolioAllocationsSyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
