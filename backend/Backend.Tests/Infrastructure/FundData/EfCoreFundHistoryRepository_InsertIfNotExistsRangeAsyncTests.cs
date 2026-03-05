using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Backend.API.Infrastructure.FundData;
using Backend.API.Infrastructure.FundData.Repositories;
using Backend.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Backend.Tests.Infrastructure.FundData;

[TestFixture]
public class EfCoreFundHistoryRepository_InsertIfNotExistsRangeAsyncTests
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

    // ===== Insert new records =====

    [Test]
    public async Task InsertIfNotExistsRangeAsync_NewRecords_InsertsAll()
    {
        // Arrange
        var records = new[]
        {
            CreateRecord(_testIsin, 100m, "2025-01-15"),
            CreateRecord(_testIsin, 101m, "2025-01-16"),
            CreateRecord(_testIsin, 102m, "2025-01-17")
        };

        // Act
        await _sut.InsertIfNotExistsRangeAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(3));
    }

    // ===== Skip existing (deduplication) =====

    [Test]
    public async Task InsertIfNotExistsRangeAsync_ExistingRecord_SkipsIt()
    {
        // Arrange — insert one record first
        var existing = new[] { CreateRecord(_testIsin, 100m, "2025-01-15") };
        await _sut.InsertIfNotExistsRangeAsync(existing);
        await _sut.SaveChangesAsync();

        // Try to insert same date again
        var duplicate = new[] { CreateRecord(_testIsin, 999m, "2025-01-15") };

        // Act
        await _sut.InsertIfNotExistsRangeAsync(duplicate);
        await _sut.SaveChangesAsync();

        // Assert — still just one record
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task InsertIfNotExistsRangeAsync_ExistingRecord_PreservesOriginalNavValue()
    {
        // Arrange — insert record with Nav = 100
        var existing = new[] { CreateRecord(_testIsin, 100m, "2025-01-15") };
        await _sut.InsertIfNotExistsRangeAsync(existing);
        await _sut.SaveChangesAsync();

        // Try to insert same date with different Nav = 999
        var duplicate = new[] { CreateRecord(_testIsin, 999m, "2025-01-15") };

        // Act
        await _sut.InsertIfNotExistsRangeAsync(duplicate);
        await _sut.SaveChangesAsync();

        // Assert — original Nav preserved
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.That(stored.Nav, Is.EqualTo(100m),
            "Original Nav must be preserved — insert-if-not-exists should not overwrite");
    }

    // ===== Mixed batch =====

    [Test]
    public async Task InsertIfNotExistsRangeAsync_MixedNewAndExisting_InsertsOnlyNew()
    {
        // Arrange — insert one existing record
        var existing = new[] { CreateRecord(_testIsin, 100m, "2025-01-15") };
        await _sut.InsertIfNotExistsRangeAsync(existing);
        await _sut.SaveChangesAsync();

        // Batch: one duplicate + one new
        var batch = new[]
        {
            CreateRecord(_testIsin, 999m, "2025-01-15"), // duplicate — skip
            CreateRecord(_testIsin, 110m, "2025-01-16")  // new — insert
        };

        // Act
        await _sut.InsertIfNotExistsRangeAsync(batch);
        await _sut.SaveChangesAsync();

        // Assert
        var records = await _context.FundHistoryRecords.OrderBy(r => r.NavDate).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(records, Has.Count.EqualTo(2));
            Assert.That(records[0].Nav, Is.EqualTo(100m), "Existing record Nav preserved");
            Assert.That(records[1].Nav, Is.EqualTo(110m), "New record inserted");
        });
    }

    // ===== Null NavDate =====

    [Test]
    public async Task InsertIfNotExistsRangeAsync_NullNavDate_SkipsRecord()
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
        await _sut.InsertIfNotExistsRangeAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    // ===== Empty input =====

    [Test]
    public async Task InsertIfNotExistsRangeAsync_EmptyCollection_NoChanges()
    {
        // Act
        await _sut.InsertIfNotExistsRangeAsync(Array.Empty<FundHistoryRecord>());
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(0));
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
