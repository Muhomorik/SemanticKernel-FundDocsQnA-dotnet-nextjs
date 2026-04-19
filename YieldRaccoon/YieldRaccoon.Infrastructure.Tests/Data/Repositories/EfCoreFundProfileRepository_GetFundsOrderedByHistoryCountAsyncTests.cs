using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Data.Repositories;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;

namespace YieldRaccoon.Infrastructure.Tests.Data.Repositories;

[TestFixture]
[TestOf(typeof(EfCoreFundProfileRepository))]
public class EfCoreFundProfileRepository_GetFundsOrderedByLastVisitAsyncTests
{
    private IFixture _fixture = null!;
    private YieldRaccoonDbContext _context = null!;
    private EfCoreFundProfileRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _context = InMemoryDbContextFactory.Create();
        _sut = new EfCoreFundProfileRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private async Task<FundProfile> CreateProfileAsync(
        string? orderbookId = "OB-DEFAULT",
        DateTimeOffset? lastVisitedAt = null,
        int historyRecordCount = 0,
        DateTimeOffset? crawlerLastUpdatedAt = null)
    {
        var fundId = _fixture.Create<IsinId>();
        var profile = new FundProfile
        {
            Id = fundId,
            Name = $"Fund {fundId.Isin}",
            FirstSeenAt = DateTimeOffset.UtcNow,
            OrderbookId = orderbookId,
            AboutFundLastVisitedAt = lastVisitedAt,
            CrawlerLastUpdatedAt = crawlerLastUpdatedAt ?? DateTimeOffset.UtcNow
        };

        await _context.FundProfiles.AddAsync(profile);
        await _context.SaveChangesAsync();

        for (var i = 0; i < historyRecordCount; i++)
        {
            await _context.FundHistoryRecords.AddAsync(new FundHistoryRecord
            {
                IsinId = fundId,
                Nav = 100m + i,
                NavDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-i))
            });
        }

        await _context.SaveChangesAsync();
        return profile;
    }

    #region Filtering

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_NoProfiles_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_NullOrderbookId_ExcludedFromResults()
    {
        // Arrange
        await CreateProfileAsync(orderbookId: null);
        await CreateProfileAsync(orderbookId: "OB-001");

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-001")));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_AllProfilesLackOrderbookId_ReturnsEmptyList()
    {
        // Arrange
        await CreateProfileAsync(orderbookId: null);
        await CreateProfileAsync(orderbookId: null);

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Projection

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_ProjectsAllFields()
    {
        // Arrange
        var visitedAt = DateTimeOffset.UtcNow;
        var profile = await CreateProfileAsync(
            orderbookId: "OB-PROJ",
            lastVisitedAt: visitedAt,
            historyRecordCount: 3);

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var item = result[0];
        Assert.That(item.Isin, Is.EqualTo(profile.Id.Isin));
        Assert.That(item.OrderBookId, Is.EqualTo(OrderBookId.Create("OB-PROJ")));
        Assert.That(item.Name, Is.EqualTo(profile.Name));
        Assert.That(item.HistoryRecordCount, Is.EqualTo(3));
        Assert.That(item.LastVisitedAt, Is.EqualTo(visitedAt));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_NullLastVisitedAt_ProjectsAsNull()
    {
        // Arrange
        await CreateProfileAsync(lastVisitedAt: null);

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result[0].LastVisitedAt, Is.Null);
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_NoHistoryRecords_CountIsZero()
    {
        // Arrange
        await CreateProfileAsync(historyRecordCount: 0);

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result[0].HistoryRecordCount, Is.EqualTo(0));
    }

    #endregion

    #region Ordering

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_NullLastVisitedAt_SortsBeforeVisited()
    {
        // Arrange
        await CreateProfileAsync(orderbookId: "OB-VISITED", lastVisitedAt: DateTimeOffset.UtcNow);
        await CreateProfileAsync(orderbookId: "OB-NEVER");

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result[0].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-NEVER")),
            "Never-visited fund should come first");
        Assert.That(result[1].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-VISITED")));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_EarlierVisit_SortsBeforeLaterVisit()
    {
        // Arrange
        var earlier = DateTimeOffset.UtcNow.AddHours(-2);
        var later = DateTimeOffset.UtcNow;

        await CreateProfileAsync(orderbookId: "OB-LATER", lastVisitedAt: later);
        await CreateProfileAsync(orderbookId: "OB-EARLIER", lastVisitedAt: earlier);

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result[0].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-EARLIER")),
            "Earlier-visited fund should come first");
        Assert.That(result[1].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-LATER")));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_NeverVisited_SortsBeforeVisited_RegardlessOfHistoryCount()
    {
        // Arrange — visited fund has fewer records, but should still sort after never-visited
        await CreateProfileAsync(orderbookId: "OB-VISITED-FEW",
            lastVisitedAt: DateTimeOffset.UtcNow, historyRecordCount: 1);
        await CreateProfileAsync(orderbookId: "OB-NEVER-MANY",
            historyRecordCount: 100);

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result[0].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-NEVER-MANY")),
            "Never-visited fund should come first regardless of history count");
    }

    #endregion

    #region Limit

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_MoreProfilesThanLimit_ReturnsLimitedCount()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
            await CreateProfileAsync(orderbookId: $"OB-{i:D3}");

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync(limit: 3);

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_FewerProfilesThanLimit_ReturnsAll()
    {
        // Arrange
        await CreateProfileAsync(orderbookId: "OB-001");
        await CreateProfileAsync(orderbookId: "OB-002");

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync(limit: 60);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
    }

    #endregion

    #region Stale crawler filter

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_CrawlerLastUpdatedAtOlderThanOneMonth_ExcludedFromResults()
    {
        // Arrange — one fund the list crawler hasn't seen in over a month (likely delisted),
        // one fund crawled yesterday.
        var stale = DateTimeOffset.UtcNow.AddMonths(-1).AddDays(-1);
        await CreateProfileAsync(orderbookId: "OB-STALE", crawlerLastUpdatedAt: stale);
        await CreateProfileAsync(orderbookId: "OB-FRESH",
            crawlerLastUpdatedAt: DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-FRESH")));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_CrawlerLastUpdatedAtExactlyOneMonthAgo_Included()
    {
        // Arrange — boundary: a fund crawled exactly one month ago should still be eligible.
        // We subtract a small buffer so the repository's own UtcNow doesn't push the row past the cutoff
        // during test execution.
        var boundary = DateTimeOffset.UtcNow.AddMonths(-1).AddSeconds(30);
        await CreateProfileAsync(orderbookId: "OB-BOUNDARY", crawlerLastUpdatedAt: boundary);

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-BOUNDARY")));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_CrawlerLastUpdatedAtNull_ExcludedFromResults()
    {
        // Arrange — null means the list crawler has never recorded this fund; treat as stale.
        // Insert directly to bypass the helper's non-null default.
        var fundId = _fixture.Create<IsinId>();
        await _context.FundProfiles.AddAsync(new FundProfile
        {
            Id = fundId,
            Name = $"Fund {fundId.Isin}",
            FirstSeenAt = DateTimeOffset.UtcNow,
            OrderbookId = "OB-NULL-CRAWLER",
            CrawlerLastUpdatedAt = null
        });
        await _context.SaveChangesAsync();
        await CreateProfileAsync(orderbookId: "OB-FRESH");

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-FRESH")));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByLastVisitAsync))]
    public async Task GetFundsOrderedByLastVisitAsync_RecentCrawl_Included()
    {
        // Arrange — positive control: a fund crawled yesterday is clearly fresh.
        await CreateProfileAsync(orderbookId: "OB-YESTERDAY",
            crawlerLastUpdatedAt: DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var result = await _sut.GetFundsOrderedByLastVisitAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OrderBookId, Is.EqualTo(OrderBookId.Create("OB-YESTERDAY")));
    }

    #endregion
}
