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
public class EfCoreFundHistoryRepository_AddRangeIfNotExistsAsyncTests
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

    private async Task<FundProfile> CreateAndSaveFundProfileAsync(IsinId? fundId = null)
    {
        fundId ??= _fixture.Create<IsinId>();
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
    public async Task AddRangeIfNotExistsAsync_AllNewRecords_InsertsAllAndReturnsCount()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var records = new[]
        {
            new FundHistoryRecord { IsinId = profile.Id, Nav = 100.0m, NavDate = new DateOnly(2024, 1, 15) },
            new FundHistoryRecord { IsinId = profile.Id, Nav = 101.0m, NavDate = new DateOnly(2024, 1, 16) },
            new FundHistoryRecord { IsinId = profile.Id, Nav = 102.0m, NavDate = new DateOnly(2024, 1, 17) }
        };

        // Act
        var insertedCount = await _sut.AddRangeIfNotExistsAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        Assert.That(insertedCount, Is.EqualTo(3));
        var totalCount = await _context.FundHistoryRecords.CountAsync();
        Assert.That(totalCount, Is.EqualTo(3));
    }

    [Test]
    public async Task AddRangeIfNotExistsAsync_AllExistingRecords_InsertsNoneAndReturnsZero()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var existingRecords = new[]
        {
            new FundHistoryRecord { IsinId = profile.Id, Nav = 100.0m, NavDate = new DateOnly(2024, 1, 15) },
            new FundHistoryRecord { IsinId = profile.Id, Nav = 101.0m, NavDate = new DateOnly(2024, 1, 16) }
        };
        await _sut.AddOrUpdateRangeAsync(existingRecords);
        await _sut.SaveChangesAsync();

        // Same fund + same dates
        var duplicateRecords = new[]
        {
            new FundHistoryRecord { IsinId = profile.Id, Nav = 999.0m, NavDate = new DateOnly(2024, 1, 15) },
            new FundHistoryRecord { IsinId = profile.Id, Nav = 999.0m, NavDate = new DateOnly(2024, 1, 16) }
        };

        // Act
        var insertedCount = await _sut.AddRangeIfNotExistsAsync(duplicateRecords);
        await _sut.SaveChangesAsync();

        // Assert
        Assert.That(insertedCount, Is.EqualTo(0));
        var totalCount = await _context.FundHistoryRecords.CountAsync();
        Assert.That(totalCount, Is.EqualTo(2), "No new records should have been added");
    }

    [Test]
    public async Task AddRangeIfNotExistsAsync_MixedNewAndExisting_InsertsOnlyNewRecords()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var existingRecord = new FundHistoryRecord
        {
            IsinId = profile.Id,
            Nav = 100.0m,
            NavDate = new DateOnly(2024, 1, 15)
        };
        await _sut.AddOrUpdateAsync(existingRecord);
        await _sut.SaveChangesAsync();

        var records = new[]
        {
            new FundHistoryRecord { IsinId = profile.Id, Nav = 999.0m, NavDate = new DateOnly(2024, 1, 15) },  // Existing — skip
            new FundHistoryRecord { IsinId = profile.Id, Nav = 101.0m, NavDate = new DateOnly(2024, 1, 16) },  // New
            new FundHistoryRecord { IsinId = profile.Id, Nav = 102.0m, NavDate = new DateOnly(2024, 1, 17) }   // New
        };

        // Act
        var insertedCount = await _sut.AddRangeIfNotExistsAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        Assert.That(insertedCount, Is.EqualTo(2));
        var totalCount = await _context.FundHistoryRecords.CountAsync();
        Assert.That(totalCount, Is.EqualTo(3));
    }

    [Test]
    public async Task AddRangeIfNotExistsAsync_ExistingRecordNavDate_DoesNotOverwriteValue()
    {
        // Arrange
        var profile = await CreateAndSaveFundProfileAsync();
        var existingRecord = new FundHistoryRecord
        {
            IsinId = profile.Id,
            Nav = 100.0m,
            NavDate = new DateOnly(2024, 1, 15)
        };
        await _sut.AddOrUpdateAsync(existingRecord);
        await _sut.SaveChangesAsync();

        var duplicateWithDifferentNav = new[]
        {
            new FundHistoryRecord { IsinId = profile.Id, Nav = 999.0m, NavDate = new DateOnly(2024, 1, 15) }
        };

        // Act
        await _sut.AddRangeIfNotExistsAsync(duplicateWithDifferentNav);
        await _sut.SaveChangesAsync();

        // Assert — original NAV preserved
        var record = await _context.FundHistoryRecords.FirstAsync();
        Assert.That(record.Nav, Is.EqualTo(100.0m), "Original NAV should not be overwritten");
    }

    [Test]
    public async Task AddRangeIfNotExistsAsync_EmptyCollection_ReturnsZero()
    {
        // Arrange
        var records = Enumerable.Empty<FundHistoryRecord>();

        // Act
        var insertedCount = await _sut.AddRangeIfNotExistsAsync(records);

        // Assert
        Assert.That(insertedCount, Is.EqualTo(0));
        var totalCount = await _context.FundHistoryRecords.CountAsync();
        Assert.That(totalCount, Is.EqualTo(0));
    }

    [Test]
    public async Task AddRangeIfNotExistsAsync_DifferentFundsSameNavDate_InsertsBoth()
    {
        // Arrange
        var profile1 = await CreateAndSaveFundProfileAsync();
        var profile2 = await CreateAndSaveFundProfileAsync();

        var records = new[]
        {
            new FundHistoryRecord { IsinId = profile1.Id, Nav = 100.0m, NavDate = new DateOnly(2024, 1, 15) },
            new FundHistoryRecord { IsinId = profile2.Id, Nav = 200.0m, NavDate = new DateOnly(2024, 1, 15) }
        };

        // Act
        var insertedCount = await _sut.AddRangeIfNotExistsAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        Assert.That(insertedCount, Is.EqualTo(2));
        var totalCount = await _context.FundHistoryRecords.CountAsync();
        Assert.That(totalCount, Is.EqualTo(2));
    }
}
