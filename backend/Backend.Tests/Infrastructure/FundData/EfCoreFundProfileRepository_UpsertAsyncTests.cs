using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Backend.API.Infrastructure.FundData;
using Backend.API.Infrastructure.FundData.Repositories;
using Backend.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Backend.Tests.Infrastructure.FundData;

[TestFixture]
public class EfCoreFundProfileRepository_UpsertAsyncTests
{
    private FundDataDbContext _context = null!;
    private EfCoreFundProfileRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _context = InMemoryFundDataDbContextFactory.Create();
        _sut = new EfCoreFundProfileRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    #region Insert

    [Test]
    public async Task UpsertAsync_NewProfile_InsertsIntoDatabase()
    {
        // Arrange
        var profile = CreateProfile("SE0008613939", "Test Fund A");

        // Act
        await _sut.UpsertAsync(profile);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundProfiles.FirstOrDefaultAsync();
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Name, Is.EqualTo("Test Fund A"));
        Assert.That(stored.Id.Isin, Is.EqualTo("SE0008613939"));
    }

    [Test]
    public async Task UpsertAsync_NewProfile_PreservesAllFields()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var profile = new FundProfile
        {
            Id = IsinId.Create("SE0008613939"),
            Name = "Full Fields Fund",
            FirstSeenAt = now,
            CrawlerLastUpdatedAt = now,
            Category = "Equity",
            CompanyName = "Test Company",
            FundType = "Aktiefond",
            Capital = 5_000_000m,
            NumberOfOwners = 42_000,
            Rating = 5,
            Risk = 4,
            ManagementFee = 0.0125m,
            TotalFee = 0.015m,
            SharpeRatio = 1.8m,
            StandardDeviation = 15.2m,
            EsgScore = 22.5m,
            Buyable = true
        };

        // Act
        await _sut.UpsertAsync(profile);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundProfiles.FindAsync(IsinId.Create("SE0008613939"));
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Category, Is.EqualTo("Equity"));
            Assert.That(stored.CompanyName, Is.EqualTo("Test Company"));
            Assert.That(stored.Capital, Is.EqualTo(5_000_000m));
            Assert.That(stored.NumberOfOwners, Is.EqualTo(42_000));
            Assert.That(stored.Rating, Is.EqualTo(5));
            Assert.That(stored.ManagementFee, Is.EqualTo(0.0125m));
            Assert.That(stored.EsgScore, Is.EqualTo(22.5m));
            Assert.That(stored.Buyable, Is.True);
        });
    }

    #endregion

    #region Update

    [Test]
    public async Task UpsertAsync_ExistingProfile_UpdatesMutableFields()
    {
        // Arrange
        var isinId = IsinId.Create("SE0008613939");
        var original = CreateProfile("SE0008613939", "Original Name");
        original.Category = "Equity";
        original.Capital = 1_000_000m;

        await _sut.UpsertAsync(original);
        await _sut.SaveChangesAsync();

        var updated = new FundProfile
        {
            Id = isinId,
            Name = "Updated Name",
            FirstSeenAt = DateTimeOffset.UtcNow, // this should NOT overwrite
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow,
            Category = "Bond",
            Capital = 2_000_000m
        };

        // Act
        await _sut.UpsertAsync(updated);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundProfiles.FindAsync(isinId);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Name, Is.EqualTo("Updated Name"));
            Assert.That(stored.Category, Is.EqualTo("Bond"));
            Assert.That(stored.Capital, Is.EqualTo(2_000_000m));
        });
    }

    #endregion

    #region FirstSeenAt Preservation

    [Test]
    public async Task UpsertAsync_ExistingProfile_PreservesFirstSeenAt()
    {
        // Arrange
        var isinId = IsinId.Create("SE0008613939");
        var originalFirstSeen = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var original = new FundProfile
        {
            Id = isinId,
            Name = "Original",
            FirstSeenAt = originalFirstSeen,
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow
        };
        await _sut.UpsertAsync(original);
        await _sut.SaveChangesAsync();

        var update = new FundProfile
        {
            Id = isinId,
            Name = "Updated",
            FirstSeenAt = DateTimeOffset.UtcNow, // new value — should be ignored
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _sut.UpsertAsync(update);
        await _sut.SaveChangesAsync();

        // Assert — FirstSeenAt must retain its original value
        var stored = await _context.FundProfiles.FindAsync(isinId);
        Assert.That(stored!.FirstSeenAt, Is.EqualTo(originalFirstSeen));
    }

    #endregion

    #region AboutFundLastVisitedAt Preservation

    [Test]
    public async Task UpsertAsync_CrawlUpdateWithNullAboutFundLastVisitedAt_PreservesExistingTimestamp()
    {
        // Arrange — original profile has AboutFundLastVisitedAt set (from a prior about-fund visit)
        var isinId = IsinId.Create("SE0008613939");
        var originalVisitedAt = new DateTimeOffset(2026, 2, 27, 14, 30, 0, TimeSpan.Zero);

        var original = new FundProfile
        {
            Id = isinId,
            Name = "Original",
            FirstSeenAt = DateTimeOffset.UtcNow,
            AboutFundLastVisitedAt = originalVisitedAt
        };
        await _sut.UpsertAsync(original);
        await _sut.SaveChangesAsync();

        // Crawl update comes in with null AboutFundLastVisitedAt (crawler doesn't set it)
        var crawlUpdate = new FundProfile
        {
            Id = isinId,
            Name = "Crawl Update",
            FirstSeenAt = DateTimeOffset.UtcNow,
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow,
            AboutFundLastVisitedAt = null // <-- crawler doesn't know about this
        };

        // Act
        await _sut.UpsertAsync(crawlUpdate);
        await _sut.SaveChangesAsync();

        // Assert — the original timestamp must survive the crawl update
        var stored = await _context.FundProfiles.FindAsync(isinId);
        Assert.That(stored!.AboutFundLastVisitedAt, Is.EqualTo(originalVisitedAt),
            "Crawler update with null AboutFundLastVisitedAt must not wipe the existing value");
    }

    [Test]
    public async Task UpsertAsync_AboutFundUpdateWithExplicitTimestamp_UpdatesAboutFundLastVisitedAt()
    {
        // Arrange — original profile has no AboutFundLastVisitedAt
        var isinId = IsinId.Create("SE0008613939");

        var original = new FundProfile
        {
            Id = isinId,
            Name = "Original",
            FirstSeenAt = DateTimeOffset.UtcNow,
            AboutFundLastVisitedAt = null
        };
        await _sut.UpsertAsync(original);
        await _sut.SaveChangesAsync();

        // About-fund orchestrator sets a new timestamp
        var visitedAt = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var aboutUpdate = new FundProfile
        {
            Id = isinId,
            Name = "About Update",
            FirstSeenAt = DateTimeOffset.UtcNow,
            AboutFundLastVisitedAt = visitedAt // <-- explicitly set by about-fund endpoint
        };

        // Act
        await _sut.UpsertAsync(aboutUpdate);
        await _sut.SaveChangesAsync();

        // Assert — the new timestamp is applied
        var stored = await _context.FundProfiles.FindAsync(isinId);
        Assert.That(stored!.AboutFundLastVisitedAt, Is.EqualTo(visitedAt));
    }

    [Test]
    public async Task UpsertAsync_AboutFundUpdateOverwritesPreviousTimestamp()
    {
        // Arrange — profile already has a visited-at timestamp from a prior visit
        var isinId = IsinId.Create("SE0008613939");
        var firstVisit = new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero);
        var secondVisit = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

        var original = new FundProfile
        {
            Id = isinId,
            Name = "Original",
            FirstSeenAt = DateTimeOffset.UtcNow,
            AboutFundLastVisitedAt = firstVisit
        };
        await _sut.UpsertAsync(original);
        await _sut.SaveChangesAsync();

        var aboutUpdate = new FundProfile
        {
            Id = isinId,
            Name = "About Update",
            FirstSeenAt = DateTimeOffset.UtcNow,
            AboutFundLastVisitedAt = secondVisit
        };

        // Act
        await _sut.UpsertAsync(aboutUpdate);
        await _sut.SaveChangesAsync();

        // Assert — second visit overwrites first
        var stored = await _context.FundProfiles.FindAsync(isinId);
        Assert.That(stored!.AboutFundLastVisitedAt, Is.EqualTo(secondVisit));
    }

    [Test]
    public async Task UpsertAsync_NewProfileWithNullAboutFundLastVisitedAt_StoresNull()
    {
        // Arrange
        var profile = new FundProfile
        {
            Id = IsinId.Create("SE0008613939"),
            Name = "Brand New Fund",
            FirstSeenAt = DateTimeOffset.UtcNow,
            AboutFundLastVisitedAt = null
        };

        // Act
        await _sut.UpsertAsync(profile);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundProfiles.FindAsync(IsinId.Create("SE0008613939"));
        Assert.That(stored!.AboutFundLastVisitedAt, Is.Null);
    }

    #endregion

    #region CrawlerLastUpdatedAt Preservation

    [Test]
    public async Task UpsertAsync_AboutUpdateWithNullCrawlerLastUpdatedAt_PreservesExistingTimestamp()
    {
        // Arrange — original profile has CrawlerLastUpdatedAt set (from a prior list sync)
        var isinId = IsinId.Create("SE0008613939");
        var originalCrawlerUpdatedAt = new DateTimeOffset(2026, 2, 27, 14, 30, 0, TimeSpan.Zero);

        var original = new FundProfile
        {
            Id = isinId,
            Name = "Original",
            FirstSeenAt = DateTimeOffset.UtcNow,
            CrawlerLastUpdatedAt = originalCrawlerUpdatedAt
        };
        await _sut.UpsertAsync(original);
        await _sut.SaveChangesAsync();

        // About-fund update comes in with null CrawlerLastUpdatedAt (about endpoint nulls it out)
        var aboutUpdate = new FundProfile
        {
            Id = isinId,
            Name = "About Update",
            FirstSeenAt = DateTimeOffset.UtcNow,
            CrawlerLastUpdatedAt = null
        };

        // Act
        await _sut.UpsertAsync(aboutUpdate);
        await _sut.SaveChangesAsync();

        // Assert — the original timestamp must survive the about-fund update
        var stored = await _context.FundProfiles.FindAsync(isinId);
        Assert.That(stored!.CrawlerLastUpdatedAt, Is.EqualTo(originalCrawlerUpdatedAt),
            "About-fund update with null CrawlerLastUpdatedAt must not wipe the existing value");
    }

    #endregion

    #region Multiple Profiles

    [Test]
    public async Task UpsertAsync_MultipleDifferentProfiles_InsertsAll()
    {
        // Arrange
        var profileA = CreateProfile("SE0008613939", "Fund A");
        var profileB = CreateProfile("LU0274208692", "Fund B");

        // Act
        await _sut.UpsertAsync(profileA);
        await _sut.UpsertAsync(profileB);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundProfiles.CountAsync();
        Assert.That(count, Is.EqualTo(2));
    }

    #endregion

    #region Helpers

    private static FundProfile CreateProfile(string isin, string name)
    {
        return new FundProfile
        {
            Id = IsinId.Create(isin),
            Name = name,
            FirstSeenAt = DateTimeOffset.UtcNow,
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
