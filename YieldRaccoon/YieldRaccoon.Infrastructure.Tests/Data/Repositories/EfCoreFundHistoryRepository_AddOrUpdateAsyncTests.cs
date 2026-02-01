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
[TestOf(typeof(EfCoreFundHistoryRepository))]
public class EfCoreFundHistoryRepository_AddOrUpdateAsyncTests
{
    private IFixture _fixture = null!;
    private YieldRaccoonDbContext _context = null!;
    private EfCoreFundHistoryRepository _sut = null!;
    private EfCoreFundProfileRepository _profileRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _context = InMemoryDbContextFactory.Create();
        _sut = new EfCoreFundHistoryRepository(_context);
        _profileRepo = new EfCoreFundProfileRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private async Task<FundProfile> CreateAndSaveFundProfileAsync(FundId? fundId = null)
    {
        fundId ??= _fixture.Create<FundId>();
        var profile = new FundProfile
        {
            Id = fundId.Value,
            Name = "Test Fund",
            FirstSeenAt = DateTimeOffset.UtcNow
        };
        await _profileRepo.AddOrUpdateAsync(profile);
        await _profileRepo.SaveChangesAsync();
        return profile;
    }

    [Test]
    public async Task AddOrUpdateAsync_NewRecord_AddsToDatabase()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var record = new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 100.0m,
            NavDate = new DateOnly(2024, 1, 15)
        };

        // Act
        await _sut.AddOrUpdateAsync(record);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task AddOrUpdateAsync_SameFundIdAndNavDate_UpdatesNav()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var record1 = new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 100.0m,
            NavDate = new DateOnly(2024, 1, 15)
        };
        var record2 = new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 105.0m,  // Different NAV
            NavDate = new DateOnly(2024, 1, 15)  // Same NavDate
        };

        // Act
        await _sut.AddOrUpdateAsync(record1);
        await _sut.SaveChangesAsync();

        await _sut.AddOrUpdateAsync(record2);  // Should update existing record
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(1), "Should have only one record - updated, not added");

        var retrieved = await _context.FundHistoryRecords.FirstAsync();
        Assert.That(retrieved.Nav, Is.EqualTo(105.0m), "NAV should be updated to new value");
    }

    [Test]
    public async Task AddOrUpdateAsync_SameFundIdDifferentNavDate_AddsBothRecords()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var record1 = new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 100.0m,
            NavDate = new DateOnly(2024, 1, 15)
        };
        var record2 = new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 101.0m,
            NavDate = new DateOnly(2024, 1, 16)  // Different NavDate
        };

        // Act
        await _sut.AddOrUpdateAsync(record1);
        await _sut.SaveChangesAsync();

        await _sut.AddOrUpdateAsync(record2);  // Different NavDate, should be added
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(2), "Records with different NavDate should both exist");
    }

    [Test]
    public async Task AddOrUpdateRangeAsync_MixedNewAndExisting_UpdatesExistingAddsNew()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();

        // Pre-add a record
        var existingRecord = new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 100.0m,
            NavDate = new DateOnly(2024, 1, 15)
        };
        await _sut.AddOrUpdateAsync(existingRecord);
        await _sut.SaveChangesAsync();

        // Prepare batch: one update (same NavDate) and two new
        var records = new[]
        {
            new FundHistoryRecord { FundId = profile.Id, Nav = 105.0m, NavDate = new DateOnly(2024, 1, 15) },  // Update existing
            new FundHistoryRecord { FundId = profile.Id, Nav = 101.0m, NavDate = new DateOnly(2024, 1, 16) },  // New
            new FundHistoryRecord { FundId = profile.Id, Nav = 102.0m, NavDate = new DateOnly(2024, 1, 17) }   // New
        };

        // Act
        await _sut.AddOrUpdateRangeAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(3), "Should have 1 updated + 2 new = 3 records");

        var updatedRecord = await _context.FundHistoryRecords
            .FirstAsync(r => r.NavDate == new DateOnly(2024, 1, 15));
        Assert.That(updatedRecord.Nav, Is.EqualTo(105.0m), "Existing record should have updated NAV");
    }

    [Test]
    public async Task AddOrUpdateRangeAsync_AllNewRecords_AddsAll()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var records = new[]
        {
            new FundHistoryRecord { FundId = profile.Id, Nav = 100.0m, NavDate = new DateOnly(2024, 1, 15) },
            new FundHistoryRecord { FundId = profile.Id, Nav = 101.0m, NavDate = new DateOnly(2024, 1, 16) },
            new FundHistoryRecord { FundId = profile.Id, Nav = 102.0m, NavDate = new DateOnly(2024, 1, 17) }
        };

        // Act
        await _sut.AddOrUpdateRangeAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task AddOrUpdateAsync_MultipleUpdatesSameNavDate_KeepsLatestValue()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();

        // Act - update same NavDate multiple times
        await _sut.AddOrUpdateAsync(new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 100.0m,
            NavDate = new DateOnly(2024, 1, 15)
        });
        await _sut.SaveChangesAsync();

        await _sut.AddOrUpdateAsync(new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 105.0m,
            NavDate = new DateOnly(2024, 1, 15)
        });
        await _sut.SaveChangesAsync();

        await _sut.AddOrUpdateAsync(new FundHistoryRecord
        {
            FundId = profile.Id,
            Nav = 110.0m,
            NavDate = new DateOnly(2024, 1, 15)
        });
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(1), "Should have exactly one record after multiple updates");

        var record = await _context.FundHistoryRecords.FirstAsync();
        Assert.That(record.Nav, Is.EqualTo(110.0m), "NAV should be latest value");
    }
}
