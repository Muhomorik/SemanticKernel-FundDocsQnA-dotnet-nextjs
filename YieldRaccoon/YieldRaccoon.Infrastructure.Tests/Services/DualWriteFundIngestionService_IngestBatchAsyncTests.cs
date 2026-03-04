using System.Reactive.Subjects;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Exceptions;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(DualWriteFundIngestionService))]
public class DualWriteFundIngestionService_IngestBatchAsyncTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private Mock<IFundIngestionService> _innerMock = null!;
    private Mock<IFundSyncApiClient> _apiClientMock = null!;
    private Subject<BackendSyncStatus> _syncSubject = null!;
    private DualWriteFundIngestionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _innerMock = _fixture.Freeze<Mock<IFundIngestionService>>();
        _apiClientMock = _fixture.Freeze<Mock<IFundSyncApiClient>>();

        _syncSubject = new Subject<BackendSyncStatus>();
        _fixture.Inject(_syncSubject);

        _sut = _fixture.Create<DualWriteFundIngestionService>();
    }

    [Test]
    public async Task IngestBatchAsync_InnerSucceeds_BackendSucceeds_ReturnsInnerCount()
    {
        // Arrange
        var funds = CreateValidFunds(3);
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 3 });

        // Act
        var result = await _sut.IngestBatchAsync(funds);

        // Assert
        Assert.That(result, Is.EqualTo(3));
        _innerMock.Verify(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundDataDto>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IngestBatchAsync_InnerSucceeds_BackendThrows_ReturnsInnerCount()
    {
        // Arrange
        var funds = CreateValidFunds(2);
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.IngestBatchAsync(funds);

        // Assert — SQLite result returned despite API failure
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public async Task IngestBatchAsync_InnerSucceeds_BackendThrows_PublishesSyncError()
    {
        // Arrange
        var funds = CreateValidFunds(1);
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Timeout"));

        BackendSyncStatus? received = null;
        _syncSubject.Subscribe(s => received = s);

        // Act
        await _sut.IngestBatchAsync(funds);
        // Allow fire-and-forget to complete
        await Task.Delay(200);

        // Assert
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.IsSuccess, Is.False);
        Assert.That(received.Message, Does.Contain("Timeout"));
    }

    [Test]
    public async Task IngestBatchAsync_InnerSucceeds_BackendSucceeds_PublishesSyncSuccess()
    {
        // Arrange
        var funds = CreateValidFunds(5);
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 5 });

        BackendSyncStatus? received = null;
        _syncSubject.Subscribe(s => received = s);

        // Act
        await _sut.IngestBatchAsync(funds);
        await Task.Delay(200);

        // Assert
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.IsSuccess, Is.True);
        Assert.That(received.Message, Does.Contain("5"));
    }

    [Test]
    public async Task IngestBatchAsync_EmptyList_SkipsBackendCall()
    {
        // Arrange
        var funds = new List<FundDataDto>();
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _sut.IngestBatchAsync(funds);
        await Task.Delay(200);

        // Assert
        _apiClientMock.Verify(
            x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task IngestBatchAsync_FundsWithNullIsin_FilteredFromApiCall()
    {
        // Arrange
        var funds = new List<FundDataDto>
        {
            new() { Isin = "SE0001234567", Name = "Valid Fund" },
            new() { Isin = null, Name = "No ISIN" },
            new() { Isin = "SE0009876543", Name = null },
            new() { Isin = "SE0005555555", Name = "Another Valid" }
        };

        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        FundListSyncRequest? capturedRequest = null;
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .Callback<FundListSyncRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 2 });

        // Act
        await _sut.IngestBatchAsync(funds);
        await Task.Delay(200);

        // Assert — only funds with both ISIN and Name should be sent
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Funds, Has.Count.EqualTo(2));
        Assert.That(capturedRequest.Funds.Select(f => f.Isin), Is.EquivalentTo(new[] { "SE0001234567", "SE0005555555" }));
    }

    [Test]
    public async Task IngestBatchAsync_BackendRateLimited_PublishesRateLimitError()
    {
        // Arrange
        var funds = CreateValidFunds(1);
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RateLimitedException(3));

        BackendSyncStatus? received = null;
        _syncSubject.Subscribe(s => received = s);

        // Act
        await _sut.IngestBatchAsync(funds);
        await Task.Delay(200);

        // Assert
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.IsSuccess, Is.False);
        Assert.That(received.Message, Does.Contain("Rate limited"));
    }

    private List<FundDataDto> CreateValidFunds(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new FundDataDto
            {
                Isin = $"SE{i:D10}",
                Name = $"Fund {i}"
            })
            .ToList();
    }
}
