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
[TestOf(typeof(CloudSyncService))]
public class CloudSyncService_SyncAsyncTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private Mock<IFundProfileRepository> _profileRepoMock = null!;
    private Mock<IFundSyncApiClient> _apiClientMock = null!;
    private CloudSyncService _sut = null!;
    private List<CloudSyncProgress> _capturedProgress = null!;
    private IProgress<CloudSyncProgress> _progress = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _profileRepoMock = _fixture.Freeze<Mock<IFundProfileRepository>>();
        _apiClientMock = _fixture.Freeze<Mock<IFundSyncApiClient>>();

        _sut = _fixture.Create<CloudSyncService>();

        _capturedProgress = [];
        _progress = new Progress<CloudSyncProgress>(p => _capturedProgress.Add(p));
    }

    [Test]
    public async Task SyncAsync_NoFundsMatch_ReturnsZeroCounts()
    {
        // Arrange
        _profileRepoMock
            .Setup(r => r.GetByCompanyNameFilterAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FundProfile>());

        // Act
        var result = await _sut.SyncAsync("NonExistent", 100, _progress);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalFunds, Is.EqualTo(0));
            Assert.That(result.ProfilesSynced, Is.EqualTo(0));
            Assert.That(result.HistoryRecordsSynced, Is.EqualTo(0));
            Assert.That(result.FailedFunds, Is.EqualTo(0));
            Assert.That(result.WasCancelled, Is.False);
        });

        _apiClientMock.Verify(
            c => c.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _apiClientMock.Verify(
            c => c.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SyncAsync_FundsFound_CallsSyncFundListWithAllProfiles()
    {
        // Arrange
        var funds = CreateFundsWithHistory(3, 0);
        SetupRepository(funds);
        SetupApiClientDefaults();

        // Act
        await _sut.SyncAsync(null, 0, _progress);

        // Assert
        _apiClientMock.Verify(
            c => c.SyncFundListAsync(
                It.Is<FundListSyncRequest>(r => r.Funds.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SyncAsync_FundsFound_CallsSyncFundAboutPerFund()
    {
        // Arrange
        var funds = CreateFundsWithHistory(3, 2);
        SetupRepository(funds);
        SetupApiClientDefaults();

        // Act
        await _sut.SyncAsync(null, 0, _progress);

        // Assert
        _apiClientMock.Verify(
            c => c.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task SyncAsync_HistoryRecordsMapped_CorrectlyConverted()
    {
        // Arrange
        var navDate = new DateOnly(2025, 6, 15);
        var fund = CreateFundWithSpecificHistory("SE0001234567", "Test Fund", 123.45m, navDate);
        SetupRepository([fund]);
        SetupApiClientDefaults();

        FundAboutSyncRequest? capturedRequest = null;
        _apiClientMock
            .Setup(c => c.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()))
            .Callback<FundAboutSyncRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", HistoryRecordsInserted = 1 });

        // Act
        await _sut.SyncAsync(null, 0, _progress);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest!.HistoryRecords, Has.Count.EqualTo(1));
            Assert.That(capturedRequest.HistoryRecords[0].Isin, Is.EqualTo("SE0001234567"));
            Assert.That(capturedRequest.HistoryRecords[0].Nav, Is.EqualTo(123.45m));
            Assert.That(capturedRequest.HistoryRecords[0].NavDate, Is.EqualTo("2025-06-15"));
        });
    }

    [Test]
    public async Task SyncAsync_ApiFailsForOneFund_ContinuesAndCountsFailure()
    {
        // Arrange
        var funds = CreateFundsWithHistory(3, 1);
        SetupRepository(funds);

        _apiClientMock
            .Setup(c => c.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 3 });

        var callCount = 0;
        _apiClientMock
            .Setup(c => c.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()))
            .Returns<FundAboutSyncRequest, CancellationToken>((_, _) =>
            {
                callCount++;
                if (callCount == 2)
                    throw new HttpRequestException("Connection refused");
                return Task.FromResult(new FundSyncResponse
                    { Success = true, Message = "OK", HistoryRecordsInserted = 1 });
            });

        // Act
        var result = await _sut.SyncAsync(null, 0, _progress);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.FailedFunds, Is.EqualTo(1));
            Assert.That(result.TotalFunds, Is.EqualTo(3));
            Assert.That(result.WasCancelled, Is.False);
        });

        // All 3 funds were attempted (failure didn't stop the loop)
        _apiClientMock.Verify(
            c => c.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task SyncAsync_CancellationRequested_StopsEarlyAndSetsWasCancelled()
    {
        // Arrange
        var funds = CreateFundsWithHistory(5, 1);
        SetupRepository(funds);

        _apiClientMock
            .Setup(c => c.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 5 });

        using var cts = new CancellationTokenSource();
        var aboutCallCount = 0;
        _apiClientMock
            .Setup(c => c.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()))
            .Returns<FundAboutSyncRequest, CancellationToken>((_, _) =>
            {
                aboutCallCount++;
                if (aboutCallCount == 2)
                    cts.Cancel(); // Cancel after 2nd fund
                return Task.FromResult(new FundSyncResponse
                    { Success = true, Message = "OK", HistoryRecordsInserted = 1 });
            });

        // Act
        var result = await _sut.SyncAsync(null, 0, _progress, cts.Token);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.WasCancelled, Is.True);
            // Should have processed at most 2 funds before cancellation was detected
            Assert.That(aboutCallCount, Is.LessThanOrEqualTo(3));
        });
    }

    [Test]
    public async Task SyncAsync_ReportsProgressForEachFund()
    {
        // Arrange
        var funds = CreateFundsWithHistory(3, 0);
        SetupRepository(funds);
        SetupApiClientDefaults();

        // Use synchronous progress to capture all reports immediately
        var progressList = new List<CloudSyncProgress>();
        var syncProgress = new SynchronousProgress<CloudSyncProgress>(p => progressList.Add(p));

        // Act
        await _sut.SyncAsync(null, 0, syncProgress);

        // Assert — should have query + profiles + per-fund + completion reports
        Assert.That(progressList, Has.Count.GreaterThanOrEqualTo(5),
            "Expected at least: querying + syncing profiles + 3 per-fund progress reports");

        // Per-fund progress should show incrementing ProcessedFunds
        var perFundReports = progressList
            .Where(p => p.Phase.StartsWith("Syncing history"))
            .ToList();
        Assert.That(perFundReports, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task SyncAsync_CompanyNameFilter_PassedToRepository()
    {
        // Arrange
        _profileRepoMock
            .Setup(r => r.GetByCompanyNameFilterAsync("Handelsbanken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FundProfile>());

        // Act
        await _sut.SyncAsync("Handelsbanken", 100, _progress);

        // Assert
        _profileRepoMock.Verify(
            r => r.GetByCompanyNameFilterAsync("Handelsbanken", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #region Helpers

    private static List<FundProfile> CreateFundsWithHistory(int fundCount, int historyPerFund)
    {
        var funds = new List<FundProfile>();
        for (var i = 0; i < fundCount; i++)
        {
            var isin = $"SE000{i:D7}";
            var fund = new FundProfile
            {
                Id = IsinId.Create(isin),
                Name = $"Fund {i}",
                CompanyName = "Test Company",
                FirstSeenAt = DateTimeOffset.UtcNow
            };

            for (var j = 0; j < historyPerFund; j++)
            {
                fund.HistoryRecords.Add(new FundHistoryRecord
                {
                    IsinId = fund.Id,
                    Nav = 100m + j,
                    NavDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-j))
                });
            }

            funds.Add(fund);
        }

        return funds;
    }

    private static FundProfile CreateFundWithSpecificHistory(string isin, string name, decimal nav, DateOnly navDate)
    {
        var fund = new FundProfile
        {
            Id = IsinId.Create(isin),
            Name = name,
            CompanyName = "Test Company",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        fund.HistoryRecords.Add(new FundHistoryRecord
        {
            IsinId = fund.Id,
            Nav = nav,
            NavDate = navDate
        });

        return fund;
    }

    private void SetupRepository(IReadOnlyList<FundProfile> funds)
    {
        _profileRepoMock
            .Setup(r => r.GetByCompanyNameFilterAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(funds);
    }

    private void SetupApiClientDefaults()
    {
        _apiClientMock
            .Setup(c => c.SyncFundListAsync(It.IsAny<FundListSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 0 });

        _apiClientMock
            .Setup(c => c.SyncFundAboutAsync(It.IsAny<FundAboutSyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundSyncResponse { Success = true, Message = "OK", HistoryRecordsInserted = 1 });
    }

    /// <summary>
    /// Synchronous progress implementation that invokes the callback immediately on the calling thread.
    /// Unlike <see cref="Progress{T}"/> which posts to the captured <see cref="SynchronizationContext"/>.
    /// </summary>
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    #endregion
}
