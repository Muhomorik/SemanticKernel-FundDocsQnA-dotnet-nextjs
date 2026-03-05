using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Backend.API.Infrastructure.FundData;
using Backend.API.Infrastructure.FundData.Repositories;
using Backend.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Backend.Tests.Infrastructure.FundData;

[TestFixture]
public class EfCoreFundHistoryRepository_UpsertRangeAsyncTests
{
    private FundDataDbContext _context = null!;
    private EfCoreFundHistoryRepository _sut = null!;
    private EfCoreFundProfileRepository _profileRepo = null!;
    private readonly IsinId _testIsin = IsinId.Create("SE0008613939");

    [SetUp]
    public async Task SetUp()
    {
        _context = InMemoryFundDataDbContextFactory.Create();
        _sut = new EfCoreFundHistoryRepository(_context);
        _profileRepo = new EfCoreFundProfileRepository(_context);

        // Insert parent profile so FK constraint is satisfied
        var profile = new FundProfile
        {
            Id = _testIsin,
            Name = "Test Fund",
            FirstSeenAt = DateTimeOffset.UtcNow
        };
        await _profileRepo.UpsertAsync(profile);
        await _profileRepo.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    // ===== Insert =====

    [Test]
    public async Task UpsertRangeAsync_NewRecords_InsertsAll()
    {
        // Arrange
        var records = new[]
        {
            CreateRecord(_testIsin, 100m, "2025-01-15"),
            CreateRecord(_testIsin, 101m, "2025-01-16"),
            CreateRecord(_testIsin, 102m, "2025-01-17")
        };

        // Act
        await _sut.UpsertRangeAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task UpsertRangeAsync_NewRecord_PreservesNavValue()
    {
        // Arrange
        var records = new[] { CreateRecord(_testIsin, 123.456m, "2025-01-15") };

        // Act
        await _sut.UpsertRangeAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.That(stored.Nav, Is.EqualTo(123.456m));
    }

    // ===== Upsert (existing record) =====

    [Test]
    public async Task UpsertRangeAsync_ExistingRecord_UpdatesNavValue()
    {
        // Arrange — insert initial record
        var initial = new[] { CreateRecord(_testIsin, 100m, "2025-01-15") };
        await _sut.UpsertRangeAsync(initial);
        await _sut.SaveChangesAsync();

        // Updated record for same date with different Nav
        var updated = new[] { CreateRecord(_testIsin, 105m, "2025-01-15") };

        // Act
        await _sut.UpsertRangeAsync(updated);
        await _sut.SaveChangesAsync();

        // Assert — single record, updated Nav
        var count = await _context.FundHistoryRecords.CountAsync();
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1), "Should still be one record (upsert, not insert)");
            Assert.That(stored.Nav, Is.EqualTo(105m), "Nav should be updated to new value");
        });
    }

    [Test]
    public async Task UpsertRangeAsync_ExistingRecord_UpdatesAllSnapshotFields()
    {
        // Arrange — insert initial record
        var initial = new[]
        {
            new FundHistoryRecord
            {
                Id = FundHistoryRecordId.New(),
                IsinId = _testIsin,
                Nav = 100m,
                NavDate = DateOnly.Parse("2025-01-15"),
                Capital = 1_000_000m,
                NumberOfOwners = 1000,
                Risk = 3,
                SharpeRatio = 1.0m,
                StandardDeviation = 10m
            }
        };
        await _sut.UpsertRangeAsync(initial);
        await _sut.SaveChangesAsync();

        // Updated record with different snapshot values
        var updated = new[]
        {
            new FundHistoryRecord
            {
                Id = FundHistoryRecordId.New(),
                IsinId = _testIsin,
                Nav = 110m,
                NavDate = DateOnly.Parse("2025-01-15"),
                Capital = 2_000_000m,
                NumberOfOwners = 2000,
                Risk = 5,
                SharpeRatio = 1.8m,
                StandardDeviation = 15m
            }
        };

        // Act
        await _sut.UpsertRangeAsync(updated);
        await _sut.SaveChangesAsync();

        // Assert — all fields updated
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Nav, Is.EqualTo(110m));
            Assert.That(stored.Capital, Is.EqualTo(2_000_000m));
            Assert.That(stored.NumberOfOwners, Is.EqualTo(2000));
            Assert.That(stored.Risk, Is.EqualTo(5));
            Assert.That(stored.SharpeRatio, Is.EqualTo(1.8m));
            Assert.That(stored.StandardDeviation, Is.EqualTo(15m));
        });
    }

    // ===== Mixed batch =====

    [Test]
    public async Task UpsertRangeAsync_MixedNewAndExisting_InsertsNewAndUpdatesExisting()
    {
        // Arrange — insert one existing record
        var existing = new[] { CreateRecord(_testIsin, 100m, "2025-01-15") };
        await _sut.UpsertRangeAsync(existing);
        await _sut.SaveChangesAsync();

        // Batch: one update (same date) + one new
        var batch = new[]
        {
            CreateRecord(_testIsin, 105m, "2025-01-15"), // update
            CreateRecord(_testIsin, 110m, "2025-01-16")  // new
        };

        // Act
        await _sut.UpsertRangeAsync(batch);
        await _sut.SaveChangesAsync();

        // Assert
        var records = await _context.FundHistoryRecords.OrderBy(r => r.NavDate).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(records, Has.Count.EqualTo(2));
            Assert.That(records[0].Nav, Is.EqualTo(105m), "Existing record should be updated");
            Assert.That(records[1].Nav, Is.EqualTo(110m), "New record should be inserted");
        });
    }

    // ===== Null NavDate =====

    [Test]
    public async Task UpsertRangeAsync_NullNavDate_SkipsRecord()
    {
        // Arrange
        var records = new[]
        {
            CreateRecord(_testIsin, 100m, "2025-01-15"),
            new FundHistoryRecord
            {
                Id = FundHistoryRecordId.New(),
                IsinId = _testIsin,
                Nav = 200m,
                NavDate = null // should be skipped
            }
        };

        // Act
        await _sut.UpsertRangeAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    // ===== Helpers =====

    private static FundHistoryRecord CreateRecord(IsinId isinId, decimal nav, string date)
    {
        return new FundHistoryRecord
        {
            Id = FundHistoryRecordId.New(),
            IsinId = isinId,
            Nav = nav,
            NavDate = DateOnly.Parse(date)
        };
    }
}
