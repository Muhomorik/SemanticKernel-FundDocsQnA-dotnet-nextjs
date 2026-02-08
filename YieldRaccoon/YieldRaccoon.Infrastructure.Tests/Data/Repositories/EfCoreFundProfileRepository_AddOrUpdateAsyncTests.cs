using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Data.Repositories;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;

namespace YieldRaccoon.Infrastructure.Tests.Data.Repositories;

[TestFixture]
[TestOf(typeof(EfCoreFundProfileRepository))]
public class EfCoreFundProfileRepository_AddOrUpdateAsyncTests
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

    [Test]
    public async Task AddOrUpdateAsync_NewProfile_AddsToDatabase()
    {
        // Arrange
        var profile = _fixture.Create<FundProfile>();

        // Act
        await _sut.AddOrUpdateAsync(profile);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundProfiles.CountAsync();
        Assert.That(count, Is.EqualTo(1));

        var retrieved = await _context.FundProfiles.FindAsync(profile.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo(profile.Name));
    }

    [Test]
    public async Task AddOrUpdateAsync_ExistingProfile_UpdatesProfile()
    {
        // Arrange
        var profile = _fixture.Create<FundProfile>();
        await _sut.AddOrUpdateAsync(profile);
        await _sut.SaveChangesAsync();

        // Create updated profile with same ID but different name
        var updatedProfile = new FundProfile
        {
            Id = profile.Id,
            Name = "Updated Name",
            FirstSeenAt = profile.FirstSeenAt,
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _sut.AddOrUpdateAsync(updatedProfile);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundProfiles.CountAsync();
        Assert.That(count, Is.EqualTo(1), "Should still have only one profile");

        var retrieved = await _context.FundProfiles.FindAsync(profile.Id);
        Assert.That(retrieved!.Name, Is.EqualTo("Updated Name"));
    }

    [Test]
    public async Task AddOrUpdateAsync_MultipleCallsSameId_MaintainsOneRecord()
    {
        // Arrange
        var profile = _fixture.Create<FundProfile>();

        // Act - call AddOrUpdate multiple times
        await _sut.AddOrUpdateAsync(profile);
        await _sut.SaveChangesAsync();

        var secondUpdate = new FundProfile
        {
            Id = profile.Id,
            Name = "Second Update",
            FirstSeenAt = profile.FirstSeenAt,
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow
        };
        await _sut.AddOrUpdateAsync(secondUpdate);
        await _sut.SaveChangesAsync();

        var thirdUpdate = new FundProfile
        {
            Id = profile.Id,
            Name = "Third Update",
            FirstSeenAt = profile.FirstSeenAt,
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow
        };
        await _sut.AddOrUpdateAsync(thirdUpdate);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundProfiles.CountAsync();
        Assert.That(count, Is.EqualTo(1), "Should have exactly one profile after multiple updates");

        var retrieved = await _context.FundProfiles.FindAsync(profile.Id);
        Assert.That(retrieved!.Name, Is.EqualTo("Third Update"));
    }
}
