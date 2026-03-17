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
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(DualWriteFundListIngestionService))]
public class DualWriteFundListIngestionService_IngestBatchAsyncTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private Mock<IFundListIngestionService> _innerMock = null!;
    private Mock<IFundSyncApiClient> _apiClientMock = null!;
    private Subject<BackendSyncStatus> _syncSubject = null!;
    private DualWriteFundListIngestionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _innerMock = _fixture.Freeze<Mock<IFundListIngestionService>>();
        _apiClientMock = _fixture.Freeze<Mock<IFundSyncApiClient>>();

        _syncSubject = new Subject<BackendSyncStatus>();
        _fixture.Inject(_syncSubject);

        _sut = _fixture.Create<DualWriteFundListIngestionService>();
    }

    [Test]
    public async Task IngestBatchAsync_InnerSucceeds_BackendSucceeds_ReturnsInnerCount()
    {
        // Arrange
        var funds = CreateValidFunds(3);
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 3 });

        // Act
        var result = await _sut.IngestBatchAsync(funds);

        // Assert
        Assert.That(result, Is.EqualTo(3));
        _innerMock.Verify(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IngestBatchAsync_InnerSucceeds_BackendThrows_ReturnsInnerCount()
    {
        // Arrange
        var funds = CreateValidFunds(2);
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
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
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
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
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
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
        var funds = new List<FundListDataDto>();
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
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
        var funds = new List<FundListDataDto>
        {
            new() { Isin = "SE0001234567", Name = "Valid Fund" },
            new() { Isin = null, Name = "No ISIN" },
            new() { Isin = "SE0009876543", Name = null },
            new() { Isin = "SE0005555555", Name = "Another Valid" }
        };

        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
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
        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
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

    [Test]
    public async Task IngestBatchAsync_ProfileExists_ApiRequestStillContainsNavAndNavDate()
    {
        // Arrange — profile exists in SQLite, DTO has Nav/NavDate snapshot
        var isin = "SE0001234567";
        var funds = new List<FundListDataDto>
        {
            new() { Isin = isin, Name = "Fund With NAV", Nav = 123.45m, NavDate = "2026-03-01" }
        };

        var profile = new FundProfile
        {
            Id = IsinId.Create(isin),
            Name = "Fund With NAV",
            FirstSeenAt = DateTimeOffset.UtcNow.AddDays(-30),
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
        };

        var profileRepoMock = _fixture.Freeze<Mock<IFundProfileRepository>>();
        profileRepoMock
            .Setup(r => r.GetByIsinAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _sut = _fixture.Create<DualWriteFundListIngestionService>();

        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        FundListSyncRequest? capturedRequest = null;
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .Callback<FundListSyncRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 1 });

        // Act
        await _sut.IngestBatchAsync(funds);
        await Task.Delay(200);

        // Assert — Nav and NavDate must come from the DTO, not be null
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Funds, Has.Count.EqualTo(1));
        Assert.That(capturedRequest.Funds[0].Nav, Is.EqualTo(123.45m));
        Assert.That(capturedRequest.Funds[0].NavDate, Is.EqualTo("2026-03-01"));
    }

    [Test]
    public async Task IngestBatchAsync_ProfileExists_ApiRequestContainsProfileTimestamps()
    {
        // Arrange — profile has authoritative timestamps that should overlay the DTO
        var isin = "SE0009876543";
        var firstSeen = new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var lastUpdated = new DateTimeOffset(2026, 3, 1, 14, 30, 0, TimeSpan.Zero);
        var lastVisited = new DateTimeOffset(2026, 3, 2, 8, 0, 0, TimeSpan.Zero);

        var funds = new List<FundListDataDto>
        {
            new() { Isin = isin, Name = "Timestamp Fund", Nav = 100m, NavDate = "2026-03-01" }
        };

        var profile = new FundProfile
        {
            Id = IsinId.Create(isin),
            Name = "Timestamp Fund",
            FirstSeenAt = firstSeen,
            CrawlerLastUpdatedAt = lastUpdated,
            AboutFundLastVisitedAt = lastVisited,
        };

        var profileRepoMock = _fixture.Freeze<Mock<IFundProfileRepository>>();
        profileRepoMock
            .Setup(r => r.GetByIsinAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _sut = _fixture.Create<DualWriteFundListIngestionService>();

        _innerMock.Setup(x => x.IngestBatchAsync(It.IsAny<IEnumerable<FundListDataDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        FundListSyncRequest? capturedRequest = null;
        _apiClientMock.Setup(x => x.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .Callback<FundListSyncRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 1 });

        // Act
        await _sut.IngestBatchAsync(funds);
        await Task.Delay(200);

        // Assert — timestamps come from the persisted profile
        Assert.That(capturedRequest, Is.Not.Null);
        var apiDto = capturedRequest!.Funds[0];
        Assert.That(apiDto.FirstSeenAt, Is.EqualTo(firstSeen.ToString("O")));
        Assert.That(apiDto.CrawlerLastUpdatedAt, Is.EqualTo(lastUpdated.ToString("O")));
        Assert.That(apiDto.AboutFundLastVisitedAt, Is.EqualTo(lastVisited.ToString("O")));
    }

    private List<FundListDataDto> CreateValidFunds(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new FundListDataDto
            {
                Isin = $"SE{i:D10}",
                Name = $"Fund {i}"
            })
            .ToList();
    }
}
