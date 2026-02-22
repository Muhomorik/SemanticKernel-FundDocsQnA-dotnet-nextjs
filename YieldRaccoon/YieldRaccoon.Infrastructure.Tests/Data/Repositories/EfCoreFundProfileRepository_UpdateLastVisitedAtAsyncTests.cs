using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.EntityFrameworkCore;
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
public class EfCoreFundProfileRepository_UpdateLastVisitedAtAsyncTests
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

    private async Task<FundProfile> CreateAndSaveFundProfileAsync(IsinId? fundId = null)
    {
        fundId ??= _fixture.Create<IsinId>();
        var profile = new FundProfile
        {
            Id = fundId.Value,
            Name = "Test Fund",
            FirstSeenAt = DateTimeOffset.UtcNow
        };
        await _sut.AddOrUpdateAsync(profile);
        await _sut.SaveChangesAsync();
        return profile;
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.UpdateLastVisitedAtAsync))]
    public async Task UpdateLastVisitedAtAsync_ExistingProfile_SetsTimestamp()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var visitedAt = DateTimeOffset.UtcNow;

        // Act
        await _sut.UpdateLastVisitedAtAsync(profile.Id, visitedAt);

        // Assert
        var updated = await _context.FundProfiles.FindAsync(profile.Id);
        Assert.That(updated!.AboutFundLastVisitedAt, Is.EqualTo(visitedAt));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.UpdateLastVisitedAtAsync))]
    public void UpdateLastVisitedAtAsync_NonExistentProfile_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = _fixture.Create<IsinId>();
        var visitedAt = DateTimeOffset.UtcNow;

        // Act & Assert
        Assert.DoesNotThrowAsync(() => _sut.UpdateLastVisitedAtAsync(nonExistentId, visitedAt));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.UpdateLastVisitedAtAsync))]
    public async Task UpdateLastVisitedAtAsync_CalledTwice_UpdatesToLatestTimestamp()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var firstVisit = DateTimeOffset.UtcNow.AddHours(-1);
        var secondVisit = DateTimeOffset.UtcNow;

        // Act
        await _sut.UpdateLastVisitedAtAsync(profile.Id, firstVisit);
        await _sut.UpdateLastVisitedAtAsync(profile.Id, secondVisit);

        // Assert
        var updated = await _context.FundProfiles.FindAsync(profile.Id);
        Assert.That(updated!.AboutFundLastVisitedAt, Is.EqualTo(secondVisit));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByHistoryCountAsync))]
    public async Task GetFundsOrderedByHistoryCountAsync_ProjectsLastVisitedAt()
    {
        // Arrange
        var visitedAt = DateTimeOffset.UtcNow;
        var profile = await CreateAndSaveFundProfileAsync();
        profile.OrderbookId = _fixture.Create<string>();
        profile.AboutFundLastVisitedAt = visitedAt;
        await _sut.SaveChangesAsync();

        // Act
        var items = await _sut.GetFundsOrderedByHistoryCountAsync();

        // Assert
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].LastVisitedAt, Is.EqualTo(visitedAt));
    }

    [Test]
    [TestOf(nameof(EfCoreFundProfileRepository.GetFundsOrderedByHistoryCountAsync))]
    public async Task GetFundsOrderedByHistoryCountAsync_NullLastVisitedAt_SortsBeforeNonNull()
    {
        // Arrange — two funds with same history count (0), one visited and one not
        var neverVisited = await CreateAndSaveFundProfileAsync();
        neverVisited.OrderbookId = "111";
        neverVisited.AboutFundLastVisitedAt = null;

        var visited = await CreateAndSaveFundProfileAsync();
        visited.OrderbookId = "222";
        visited.AboutFundLastVisitedAt = DateTimeOffset.UtcNow;

        await _sut.SaveChangesAsync();

        // Act
        var items = await _sut.GetFundsOrderedByHistoryCountAsync();

        // Assert
        Assert.That(items, Has.Count.EqualTo(2));
        Assert.That(items[0].LastVisitedAt, Is.Null, "Never-visited fund should come first");
        Assert.That(items[1].LastVisitedAt, Is.Not.Null, "Visited fund should come second");
    }
}
