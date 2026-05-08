using AutoFixture;
using AutoFixture.AutoMoq;
using Backend.API.ApplicationCore.DTOs;
using Backend.API.ApplicationCore.Services;
using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Backend.Tests.ApplicationCore.Services;

[TestFixture]
[Category("Unit")]
[Category("FundData")]
public class FundSyncService_SyncPortfolioAllocationsAsyncTests
{
    private IFixture _fixture = null!;
    private Mock<ICountryRepository> _countryRepoMock = null!;
    private Mock<ISectorRepository> _sectorRepoMock = null!;
    private Mock<IFundCountryAllocationRepository> _countryAllocRepoMock = null!;
    private Mock<IFundSectorAllocationRepository> _sectorAllocRepoMock = null!;
    private FundSyncService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _countryRepoMock = _fixture.Freeze<Mock<ICountryRepository>>();
        _sectorRepoMock = _fixture.Freeze<Mock<ISectorRepository>>();
        _countryAllocRepoMock = _fixture.Freeze<Mock<IFundCountryAllocationRepository>>();
        _sectorAllocRepoMock = _fixture.Freeze<Mock<IFundSectorAllocationRepository>>();

        _sut = new FundSyncService(
            _fixture.Freeze<Mock<IFundProfileRepository>>().Object,
            _fixture.Freeze<Mock<IFundHistoryRepository>>().Object,
            _countryRepoMock.Object,
            _sectorRepoMock.Object,
            _countryAllocRepoMock.Object,
            _sectorAllocRepoMock.Object,
            Mock.Of<ILogger<FundSyncService>>());
    }

    [Test]
    public async Task SyncPortfolioAllocationsAsync_InvalidIsin_ReturnsFailure()
    {
        var request = new FundPortfolioAllocationsSyncRequest
        {
            Isin = "BAD-ISIN!!",
            Countries = [],
            Sectors = []
        };

        var result = await _sut.SyncPortfolioAllocationsAsync(request);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Invalid ISIN"));
    }

    [Test]
    public async Task SyncPortfolioAllocationsAsync_NewFund_InsertsAllRows()
    {
        var isin = "SE0008613939";
        var country = new Country { Id = CountryId.New(), DisplayName = "USA", CountryCode = "US" };
        var sector = new Sector { Id = SectorId.New(), DisplayName = "Teknik" };

        _countryRepoMock.Setup(r => r.GetOrCreateAsync("USA", "US", It.IsAny<CancellationToken>()))
            .ReturnsAsync(country);
        _sectorRepoMock.Setup(r => r.GetOrCreateAsync("Teknik", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sector);
        _countryAllocRepoMock.Setup(r => r.GetByFundAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sectorAllocRepoMock.Setup(r => r.GetByFundAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var request = new FundPortfolioAllocationsSyncRequest
        {
            Isin = isin,
            Countries = [new ApiCountryAllocationDto { DisplayName = "USA", CountryCode = "US", Percentage = 50.0 }],
            Sectors = [new ApiSectorAllocationDto { DisplayName = "Teknik", Percentage = 50.0 }]
        };

        var result = await _sut.SyncPortfolioAllocationsAsync(request);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HistoryRecordsInserted, Is.EqualTo(2)); // 1 country + 1 sector

        _countryAllocRepoMock.Verify(r => r.AddAsync(It.IsAny<FundCountryAllocation>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _sectorAllocRepoMock.Verify(r => r.AddAsync(It.IsAny<FundSectorAllocation>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _countryAllocRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SyncPortfolioAllocationsAsync_RemovedSector_CallsRemoveRange()
    {
        var isin = "SE0008613939";
        var sectorTeknik = new Sector { Id = SectorId.New(), DisplayName = "Teknik" };
        var sectorRavaror = new Sector { Id = SectorId.New(), DisplayName = "Råvaror" };

        var existingRavarorAlloc = new FundSectorAllocation
        {
            Id = FundSectorAllocationId.New(),
            IsinId = IsinId.Create(isin),
            SectorId = sectorRavaror.Id,
            Percentage = 30.0m
        };

        _countryAllocRepoMock.Setup(r => r.GetByFundAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sectorAllocRepoMock.Setup(r => r.GetByFundAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingRavarorAlloc]);
        _sectorRepoMock.Setup(r => r.GetOrCreateAsync("Teknik", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sectorTeknik);

        // New payload only has Teknik — Råvaror dropped
        var request = new FundPortfolioAllocationsSyncRequest
        {
            Isin = isin,
            Countries = [],
            Sectors = [new ApiSectorAllocationDto { DisplayName = "Teknik", Percentage = 100.0 }]
        };

        await _sut.SyncPortfolioAllocationsAsync(request);

        _sectorAllocRepoMock.Verify(r => r.RemoveRangeAsync(
            It.Is<IEnumerable<FundSectorAllocation>>(coll => coll.Contains(existingRavarorAlloc)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SyncPortfolioAllocationsAsync_EmptyArrays_StillSucceeds()
    {
        _countryAllocRepoMock.Setup(r => r.GetByFundAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sectorAllocRepoMock.Setup(r => r.GetByFundAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var request = new FundPortfolioAllocationsSyncRequest
        {
            Isin = "SE0008613939",
            Countries = [],
            Sectors = []
        };

        var result = await _sut.SyncPortfolioAllocationsAsync(request);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HistoryRecordsInserted, Is.Zero);
        _countryAllocRepoMock.Verify(r => r.AddAsync(It.IsAny<FundCountryAllocation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
