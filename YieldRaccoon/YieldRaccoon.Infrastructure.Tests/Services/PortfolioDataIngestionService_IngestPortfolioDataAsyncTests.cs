using Microsoft.EntityFrameworkCore;
using Moq;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Data.Repositories;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(PortfolioDataIngestionService))]
public class PortfolioDataIngestionService_IngestPortfolioDataAsyncTests
{
    private const string SamplePayload = """
        {
          "countryChartData": [
            { "name": "USA", "y": 36.93, "countryCode": "US" },
            { "name": "Kanada", "y": 9.37, "countryCode": "CA" }
          ],
          "sectorChartData": [
            { "name": "Teknik", "y": 46.93 },
            { "name": "Råvaror", "y": 35.92 }
          ]
        }
        """;

    private YieldRaccoonDbContext _context = null!;
    private Mock<ILogger> _loggerMock = null!;
    private Mock<IFundProfileRepository> _profileRepoMock = null!;
    private PortfolioDataIngestionService _sut = null!;
    private IsinId _isinId;

    [SetUp]
    public void SetUp()
    {
        _context = InMemoryDbContextFactory.Create();
        _loggerMock = new Mock<ILogger>();
        _profileRepoMock = new Mock<IFundProfileRepository>();
        _isinId = IsinId.Create("SE0008613939");

        // Default: profile exists. Tests that need the FK guard override this.
        _profileRepoMock
            .Setup(r => r.ExistsByIsinAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new PortfolioDataIngestionService(
            _loggerMock.Object,
            _context,
            _profileRepoMock.Object,
            new EfCoreCountryRepository(_context),
            new EfCoreSectorRepository(_context),
            new EfCoreFundCountryAllocationRepository(_context),
            new EfCoreFundSectorAllocationRepository(_context));
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static AboutFundPageData PageDataWithJson(string? json) => new()
    {
        OrderBookId = OrderBookId.Create("950780"),
        PortfolioDataJson = json
    };

    [Test]
    public async Task IngestPortfolioDataAsync_NullPortfolioJson_ReturnsZero()
    {
        var result = await _sut.IngestPortfolioDataAsync(PageDataWithJson(null), _isinId);

        Assert.That(result, Is.Zero);
        _profileRepoMock.Verify(
            r => r.ExistsByIsinAsync(It.IsAny<IsinId>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "FK guard should not run when there's no payload");
    }

    [Test]
    public async Task IngestPortfolioDataAsync_FundNotProfiled_ReturnsZeroAndSkips()
    {
        // Override default — pretend the fund is unknown to the system.
        _profileRepoMock
            .Setup(r => r.ExistsByIsinAsync(_isinId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);

        Assert.That(result, Is.Zero);
        Assert.That(await _context.FundCountryAllocations.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task IngestPortfolioDataAsync_NewFund_InsertsAllAllocationsAndLookups()
    {
        await SeedFundProfile(_isinId);

        var result = await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);

        // 2 country inserts + 2 sector inserts = 4 rows touched
        Assert.That(result, Is.EqualTo(4));
        Assert.That(await _context.Countries.CountAsync(), Is.EqualTo(2));
        Assert.That(await _context.Sectors.CountAsync(), Is.EqualTo(2));
        Assert.That(await _context.FundCountryAllocations.CountAsync(), Is.EqualTo(2));
        Assert.That(await _context.FundSectorAllocations.CountAsync(), Is.EqualTo(2));

        var usa = await _context.Countries.FirstAsync(c => c.DisplayName == "USA");
        var usaAlloc = await _context.FundCountryAllocations
            .FirstAsync(a => a.IsinId == _isinId && a.CountryId == usa.Id);
        Assert.That(usaAlloc.Percentage, Is.EqualTo(36.93m));
    }

    [Test]
    public async Task IngestPortfolioDataAsync_RecrawlSamePayload_NoChangeOnIdempotentReingest()
    {
        await SeedFundProfile(_isinId);

        await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);
        var firstCountryCount = await _context.FundCountryAllocations.CountAsync();
        var firstSectorCount = await _context.FundSectorAllocations.CountAsync();

        // Re-ingest identical payload
        var rowsTouched = await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);

        Assert.That(rowsTouched, Is.Zero, "Identical payload should produce no inserts/updates/deletes");
        Assert.That(await _context.FundCountryAllocations.CountAsync(), Is.EqualTo(firstCountryCount));
        Assert.That(await _context.FundSectorAllocations.CountAsync(), Is.EqualTo(firstSectorCount));
    }

    [Test]
    public async Task IngestPortfolioDataAsync_PercentageChanged_UpdatesExistingRow()
    {
        await SeedFundProfile(_isinId);
        await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);

        var updatedPayload = """
            {
              "countryChartData": [
                { "name": "USA", "y": 50.00, "countryCode": "US" },
                { "name": "Kanada", "y": 9.37, "countryCode": "CA" }
              ],
              "sectorChartData": [
                { "name": "Teknik", "y": 46.93 },
                { "name": "Råvaror", "y": 35.92 }
              ]
            }
            """;

        var rowsTouched = await _sut.IngestPortfolioDataAsync(PageDataWithJson(updatedPayload), _isinId);

        Assert.That(rowsTouched, Is.EqualTo(1), "Only USA percentage changed");
        var usa = await _context.Countries.FirstAsync(c => c.DisplayName == "USA");
        var alloc = await _context.FundCountryAllocations
            .FirstAsync(a => a.IsinId == _isinId && a.CountryId == usa.Id);
        Assert.That(alloc.Percentage, Is.EqualTo(50.00m));
    }

    [Test]
    public async Task IngestPortfolioDataAsync_RemovedSector_DeletesAllocationKeepsLookup()
    {
        await SeedFundProfile(_isinId);
        await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);

        // Drop "Råvaror" from the new payload
        var droppedPayload = """
            {
              "countryChartData": [
                { "name": "USA", "y": 36.93, "countryCode": "US" },
                { "name": "Kanada", "y": 9.37, "countryCode": "CA" }
              ],
              "sectorChartData": [
                { "name": "Teknik", "y": 100.00 }
              ]
            }
            """;

        var rowsTouched = await _sut.IngestPortfolioDataAsync(PageDataWithJson(droppedPayload), _isinId);

        // 1 sector update (Teknik) + 1 sector delete (Råvaror) = 2 rows
        Assert.That(rowsTouched, Is.EqualTo(2));
        Assert.That(await _context.FundSectorAllocations.CountAsync(a => a.IsinId == _isinId),
            Is.EqualTo(1), "Råvaror allocation row should be deleted");
        Assert.That(await _context.Sectors.CountAsync(), Is.EqualTo(2),
            "Sector lookup row for Råvaror should be preserved");
    }

    [Test]
    public async Task IngestPortfolioDataAsync_CountryCodeBackfill_FillsNullOnRecrawl()
    {
        await SeedFundProfile(_isinId);

        // First crawl: country has no code
        var noCodePayload = """
            {
              "countryChartData": [{ "name": "USA", "y": 50.0, "countryCode": null }],
              "sectorChartData": []
            }
            """;
        await _sut.IngestPortfolioDataAsync(PageDataWithJson(noCodePayload), _isinId);
        var country = await _context.Countries.FirstAsync(c => c.DisplayName == "USA");
        Assert.That(country.CountryCode, Is.Null);

        // Second crawl: country has code "US" — should backfill
        var withCodePayload = """
            {
              "countryChartData": [{ "name": "USA", "y": 50.0, "countryCode": "US" }],
              "sectorChartData": []
            }
            """;
        await _sut.IngestPortfolioDataAsync(PageDataWithJson(withCodePayload), _isinId);

        var refreshed = await _context.Countries.AsNoTracking()
            .FirstAsync(c => c.DisplayName == "USA");
        Assert.That(refreshed.CountryCode, Is.EqualTo("US"));
    }

    [Test]
    public async Task IngestPortfolioDataAsync_MalformedJson_LogsAndReturnsZero()
    {
        await SeedFundProfile(_isinId);

        var result = await _sut.IngestPortfolioDataAsync(PageDataWithJson("{ not valid json"), _isinId);

        Assert.That(result, Is.Zero);
        Assert.That(await _context.Countries.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task IngestPortfolioDataAsync_EmptyArrays_DoesNothing()
    {
        await SeedFundProfile(_isinId);

        var emptyPayload = """{ "countryChartData": [], "sectorChartData": [] }""";
        var result = await _sut.IngestPortfolioDataAsync(PageDataWithJson(emptyPayload), _isinId);

        Assert.That(result, Is.Zero);
        Assert.That(await _context.FundCountryAllocations.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task IngestPortfolioDataAsync_LookupExists_ReusesByDisplayName()
    {
        await SeedFundProfile(_isinId);
        var otherIsin = IsinId.Create("LU0274208692");
        await SeedFundProfile(otherIsin);

        // First fund's ingestion creates Country/Sector lookups
        await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), _isinId);
        var initialCountries = await _context.Countries.CountAsync();

        // Second fund references the same names — should reuse lookups
        await _sut.IngestPortfolioDataAsync(PageDataWithJson(SamplePayload), otherIsin);

        Assert.That(await _context.Countries.CountAsync(), Is.EqualTo(initialCountries),
            "Reusing the same names must not create new lookup rows");
        Assert.That(await _context.FundCountryAllocations.CountAsync(), Is.EqualTo(4),
            "Both funds should have 2 country allocations each");
    }

    private async Task SeedFundProfile(IsinId isinId)
    {
        _context.FundProfiles.Add(new FundProfile
        {
            Id = isinId,
            Name = $"Test Fund {isinId.Isin}",
            FirstSeenAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}
