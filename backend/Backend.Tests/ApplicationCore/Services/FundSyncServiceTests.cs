using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.ApplicationCore.DTOs;
using Backend.API.ApplicationCore.Services;
using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Backend.Tests.ApplicationCore.Services;

[TestFixture]
[Category("Unit")]
[Category("FundData")]
public class FundSyncServiceTests
{
    private IFixture _fixture;
    private Mock<IFundProfileRepository> _profileRepoMock;
    private Mock<IFundHistoryRepository> _historyRepoMock;
    private Mock<ICountryRepository> _countryRepoMock;
    private Mock<ISectorRepository> _sectorRepoMock;
    private Mock<IFundCountryAllocationRepository> _countryAllocRepoMock;
    private Mock<IFundSectorAllocationRepository> _sectorAllocRepoMock;
    private FundSyncService _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _profileRepoMock = _fixture.Freeze<Mock<IFundProfileRepository>>();
        _historyRepoMock = _fixture.Freeze<Mock<IFundHistoryRepository>>();
        _countryRepoMock = _fixture.Freeze<Mock<ICountryRepository>>();
        _sectorRepoMock = _fixture.Freeze<Mock<ISectorRepository>>();
        _countryAllocRepoMock = _fixture.Freeze<Mock<IFundCountryAllocationRepository>>();
        _sectorAllocRepoMock = _fixture.Freeze<Mock<IFundSectorAllocationRepository>>();

        _sut = new FundSyncService(
            _profileRepoMock.Object,
            _historyRepoMock.Object,
            _countryRepoMock.Object,
            _sectorRepoMock.Object,
            _countryAllocRepoMock.Object,
            _sectorAllocRepoMock.Object,
            Mock.Of<ILogger<FundSyncService>>());
    }

    #region SyncFromFundListAsync

    [Test]
    public async Task SyncFromFundListAsync_ValidFunds_UpsertProfilesAndHistory()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = new List<ApiFundDto>
            {
                CreateValidFundDto("SE0008613939", "Fund A"),
                CreateValidFundDto("LU0274208692", "Fund B")
            }
        };

        // Act
        var result = await _sut.SyncFromFundListAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ProfilesProcessed, Is.EqualTo(2));
            Assert.That(result.HistoryRecordsInserted, Is.EqualTo(2));
        });

        _profileRepoMock.Verify(r => r.UpsertAsync(
            It.IsAny<FundProfile>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _historyRepoMock.Verify(r => r.UpsertRangeAsync(
            It.IsAny<IEnumerable<FundHistoryRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SyncFromFundListAsync_SkipsMissingIsin()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = new List<ApiFundDto>
            {
                CreateValidFundDto("SE0008613939", "Valid Fund"),
                new ApiFundDto { Isin = "", Name = "No ISIN Fund" }
            }
        };

        // Act
        var result = await _sut.SyncFromFundListAsync(request);

        // Assert
        Assert.That(result.ProfilesProcessed, Is.EqualTo(1));
    }

    [Test]
    public async Task SyncFromFundListAsync_SkipsMissingName()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = new List<ApiFundDto>
            {
                CreateValidFundDto("SE0008613939", "Valid Fund"),
                new ApiFundDto { Isin = "LU0274208692", Name = "" }
            }
        };

        // Act
        var result = await _sut.SyncFromFundListAsync(request);

        // Assert
        Assert.That(result.ProfilesProcessed, Is.EqualTo(1));
    }

    [Test]
    public async Task SyncFromFundListAsync_SkipsInvalidIsinFormat()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = new List<ApiFundDto>
            {
                CreateValidFundDto("SE0008613939", "Valid Fund"),
                new ApiFundDto { Isin = "INVALID", Name = "Bad ISIN Fund" }
            }
        };

        // Act
        var result = await _sut.SyncFromFundListAsync(request);

        // Assert
        Assert.That(result.ProfilesProcessed, Is.EqualTo(1));
    }

    [Test]
    public async Task SyncFromFundListAsync_EmptyList_ReturnsZeroCounts()
    {
        // Arrange
        var request = new FundListSyncRequest { Funds = new List<ApiFundDto>() };

        // Act
        var result = await _sut.SyncFromFundListAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ProfilesProcessed, Is.EqualTo(0));
        });
    }

    #endregion

    #region SyncFromFundAboutAsync

    [Test]
    public async Task SyncFromFundAboutAsync_ValidProfile_UpsertsProfileAndHistory()
    {
        // Arrange
        var request = new FundAboutSyncRequest
        {
            Profile = CreateValidFundDto("SE0008613939", "Test Fund"),
            HistoryRecords = new List<ApiFundHistoryPointDto>
            {
                new() { Isin = "SE0008613939", Nav = 123.45m, NavDate = "2025-01-15" },
                new() { Isin = "SE0008613939", Nav = 124.00m, NavDate = "2025-01-16" }
            }
        };

        // Act
        var result = await _sut.SyncFromFundAboutAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ProfilesProcessed, Is.EqualTo(1));
            Assert.That(result.HistoryRecordsInserted, Is.EqualTo(2));
        });

        _profileRepoMock.Verify(r => r.UpsertAsync(
            It.IsAny<FundProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _historyRepoMock.Verify(r => r.InsertIfNotExistsRangeAsync(
            It.IsAny<IEnumerable<FundHistoryRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SyncFromFundAboutAsync_MissingIsin_ReturnsFailed()
    {
        // Arrange
        var request = new FundAboutSyncRequest
        {
            Profile = new ApiFundDto { Isin = "", Name = "Test Fund" }
        };

        // Act
        var result = await _sut.SyncFromFundAboutAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task SyncFromFundAboutAsync_InvalidIsinFormat_ReturnsFailed()
    {
        // Arrange
        var request = new FundAboutSyncRequest
        {
            Profile = new ApiFundDto { Isin = "INVALID", Name = "Test Fund" }
        };

        // Act
        var result = await _sut.SyncFromFundAboutAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task SyncFromFundAboutAsync_SkipsHistoryWithNullNav()
    {
        // Arrange
        var request = new FundAboutSyncRequest
        {
            Profile = CreateValidFundDto("SE0008613939", "Test Fund"),
            HistoryRecords = new List<ApiFundHistoryPointDto>
            {
                new() { Isin = "SE0008613939", Nav = 123.45m, NavDate = "2025-01-15" },
                new() { Isin = "SE0008613939", Nav = null, NavDate = "2025-01-16" }
            }
        };

        // Act
        var result = await _sut.SyncFromFundAboutAsync(request);

        // Assert
        Assert.That(result.HistoryRecordsInserted, Is.EqualTo(1));
    }

    [Test]
    public async Task SyncFromFundAboutAsync_SkipsHistoryWithInvalidDate()
    {
        // Arrange
        var request = new FundAboutSyncRequest
        {
            Profile = CreateValidFundDto("SE0008613939", "Test Fund"),
            HistoryRecords = new List<ApiFundHistoryPointDto>
            {
                new() { Isin = "SE0008613939", Nav = 123.45m, NavDate = "2025-01-15" },
                new() { Isin = "SE0008613939", Nav = 124.00m, NavDate = "not-a-date" }
            }
        };

        // Act
        var result = await _sut.SyncFromFundAboutAsync(request);

        // Assert
        Assert.That(result.HistoryRecordsInserted, Is.EqualTo(1));
    }

    [Test]
    public async Task SyncFromFundAboutAsync_DoesNotSetTimestamps()
    {
        // Arrange
        FundProfile? capturedProfile = null;
        _profileRepoMock
            .Setup(r => r.UpsertAsync(It.IsAny<FundProfile>(), It.IsAny<CancellationToken>()))
            .Callback<FundProfile, CancellationToken>((p, _) => capturedProfile = p);

        var request = new FundAboutSyncRequest
        {
            Profile = CreateValidFundDto("SE0008613939", "Test Fund")
        };

        // Act
        await _sut.SyncFromFundAboutAsync(request);

        // Assert — about endpoint explicitly nulls out timestamps so the repository preserves existing values
        Assert.Multiple(() =>
        {
            Assert.That(capturedProfile?.CrawlerLastUpdatedAt, Is.Null,
                "About endpoint must null CrawlerLastUpdatedAt so repository preserves existing value");
            Assert.That(capturedProfile?.AboutFundLastVisitedAt, Is.Null,
                "About endpoint must null AboutFundLastVisitedAt so repository preserves existing value");
        });
    }

    #endregion

    #region SyncFullHistoryAsync

    [Test]
    public async Task SyncFullHistoryAsync_ValidRequest_CallsInsertIfNotExistsAndUpsertSparse()
    {
        // Arrange
        var request = new FundFullHistorySyncRequest
        {
            Profile = CreateValidProfileMetadataDto("SE0008613939", "Test Fund"),
            HistoryRecords = new List<ApiFundFullHistoryRecordDto>
            {
                new() { Isin = "SE0008613939", Nav = 100m, NavDate = "2025-01-15",
                         Capital = 1_000_000m, NumberOfOwners = 500, Risk = 3 },
                new() { Isin = "SE0008613939", Nav = 101m, NavDate = "2025-01-16",
                         Capital = 1_100_000m }
            }
        };

        // Act
        var result = await _sut.SyncFullHistoryAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ProfilesProcessed, Is.EqualTo(1));
            Assert.That(result.HistoryRecordsInserted, Is.EqualTo(2));
        });

        _profileRepoMock.Verify(r => r.InsertIfNotExistsAsync(
            It.IsAny<FundProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _historyRepoMock.Verify(r => r.UpsertSparseRangeAsync(
            It.IsAny<IEnumerable<FundHistoryRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SyncFullHistoryAsync_MissingIsin_ReturnsFailed()
    {
        // Arrange
        var request = new FundFullHistorySyncRequest
        {
            Profile = new ApiFundFullSyncProfileMetadataDto { Isin = "", Name = "Test Fund" }
        };

        // Act
        var result = await _sut.SyncFullHistoryAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        _profileRepoMock.Verify(r => r.InsertIfNotExistsAsync(
            It.IsAny<FundProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SyncFullHistoryAsync_InvalidIsinFormat_ReturnsFailed()
    {
        // Arrange
        var request = new FundFullHistorySyncRequest
        {
            Profile = new ApiFundFullSyncProfileMetadataDto { Isin = "INVALID", Name = "Test Fund" }
        };

        // Act
        var result = await _sut.SyncFullHistoryAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task SyncFullHistoryAsync_SkipsHistoryWithNullNavDate()
    {
        // Arrange
        var request = new FundFullHistorySyncRequest
        {
            Profile = CreateValidProfileMetadataDto("SE0008613939", "Test Fund"),
            HistoryRecords = new List<ApiFundFullHistoryRecordDto>
            {
                new() { Isin = "SE0008613939", Nav = 100m, NavDate = "2025-01-15" },
                new() { Isin = "SE0008613939", Nav = 101m, NavDate = null }  // null date — skip
            }
        };

        // Act
        var result = await _sut.SyncFullHistoryAsync(request);

        // Assert
        Assert.That(result.HistoryRecordsInserted, Is.EqualTo(1));
    }

    [Test]
    public async Task SyncFullHistoryAsync_DoesNotCallUpsertAsync()
    {
        // Arrange — full-sync must NOT use UpsertAsync (which would destroy existing profile data)
        var request = new FundFullHistorySyncRequest
        {
            Profile = CreateValidProfileMetadataDto("SE0008613939", "Test Fund")
        };

        // Act
        await _sut.SyncFullHistoryAsync(request);

        // Assert — UpsertAsync must never be called
        _profileRepoMock.Verify(r => r.UpsertAsync(
            It.IsAny<FundProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SyncFullHistoryAsync_EmptyHistoryRecords_StillSucceeds()
    {
        // Arrange
        var request = new FundFullHistorySyncRequest
        {
            Profile = CreateValidProfileMetadataDto("SE0008613939", "Test Fund"),
            HistoryRecords = []
        };

        // Act
        var result = await _sut.SyncFullHistoryAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HistoryRecordsInserted, Is.EqualTo(0));
        });
    }

    #endregion

    #region Helpers

    private static ApiFundDto CreateValidFundDto(string isin, string name)
    {
        return new ApiFundDto
        {
            Isin = isin,
            Name = name,
            Nav = 100.50m,
            NavDate = "2025-01-15",
            Category = "Equity",
            Capital = 1000000m,
            FirstSeenAt = "2025-01-01T00:00:00+00:00",
            CrawlerLastUpdatedAt = "2025-01-15T12:00:00+00:00",
        };
    }

    private static ApiFundFullSyncProfileMetadataDto CreateValidProfileMetadataDto(string isin, string name)
    {
        return new ApiFundFullSyncProfileMetadataDto
        {
            Isin = isin,
            Name = name,
            Category = "Equity",
            FirstSeenAt = "2025-01-01T00:00:00+00:00",
            CrawlerLastUpdatedAt = "2025-01-15T12:00:00+00:00",
        };
    }

    #endregion
}
