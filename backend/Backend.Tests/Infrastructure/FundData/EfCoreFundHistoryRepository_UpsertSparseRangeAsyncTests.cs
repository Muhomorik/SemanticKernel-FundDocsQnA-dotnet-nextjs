using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Backend.API.Infrastructure.FundData;
using Backend.API.Infrastructure.FundData.Repositories;
using Backend.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Backend.Tests.Infrastructure.FundData;

[TestFixture]
[Category("Unit")]
[Category("FundData.Database")]
public class EfCoreFundHistoryRepository_UpsertSparseRangeAsyncTests
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

    #region Insert (new records)

    [Test]
    public async Task UpsertSparseRangeAsync_NewRecords_InsertsAll()
    {
        // Arrange
        var records = new[]
        {
            CreateRecord(_testIsin, new DateOnly(2025, 1, 1), nav: 100m, capital: 1_000_000m),
            CreateRecord(_testIsin, new DateOnly(2025, 1, 2), nav: 101m, capital: 1_100_000m)
        };

        // Act
        await _sut.UpsertSparseRangeAsync(records);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task UpsertSparseRangeAsync_NewRecord_StoresAllFields()
    {
        // Arrange
        var record = CreateRecord(
            _testIsin,
            new DateOnly(2025, 1, 1),
            nav: 123.45m,
            capital: 5_000_000m,
            numberOfOwners: 9999,
            risk: 4,
            sharpeRatio: 1.75m,
            standardDeviation: 12.5m);

        // Act
        await _sut.UpsertSparseRangeAsync([record]);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Nav, Is.EqualTo(123.45m));
            Assert.That(stored.NavDate, Is.EqualTo(new DateOnly(2025, 1, 1)));
            Assert.That(stored.Capital, Is.EqualTo(5_000_000m));
            Assert.That(stored.NumberOfOwners, Is.EqualTo(9999));
            Assert.That(stored.Risk, Is.EqualTo(4));
            Assert.That(stored.SharpeRatio, Is.EqualTo(1.75m));
            Assert.That(stored.StandardDeviation, Is.EqualTo(12.5m));
        });
    }

    [Test]
    public async Task UpsertSparseRangeAsync_EmptyList_DoesNothing()
    {
        // Act
        await _sut.UpsertSparseRangeAsync([]);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task UpsertSparseRangeAsync_RecordWithNullNavDate_IsSkipped()
    {
        // Arrange — NavDate=null is the sentinel for "no date yet, skip"
        var record = new FundHistoryRecord
        {
            IsinId = _testIsin,
            Nav = 100m,
            NavDate = null
        };

        // Act
        await _sut.UpsertSparseRangeAsync([record]);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    #endregion

    #region Update (existing records — sparse COALESCE semantics)

    [Test]
    public async Task UpsertSparseRangeAsync_ExistingRecord_WithAllNonNullFields_UpdatesAll()
    {
        // Arrange — seed a record with initial values
        var navDate = new DateOnly(2025, 3, 1);
        var original = CreateRecord(_testIsin, navDate,
            nav: 100m, capital: 1_000_000m, numberOfOwners: 500, risk: 3,
            sharpeRatio: 1.0m, standardDeviation: 10m);
        await _sut.UpsertSparseRangeAsync([original]);
        await _sut.SaveChangesAsync();

        // Act — update with new non-null values
        var update = CreateRecord(_testIsin, navDate,
            nav: 999m,          // should NOT overwrite
            capital: 2_000_000m,
            numberOfOwners: 800,
            risk: 4,
            sharpeRatio: 1.5m,
            standardDeviation: 14m);
        await _sut.UpsertSparseRangeAsync([update]);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Nav, Is.EqualTo(100m), "Nav must not be overwritten");
            Assert.That(stored.NavDate, Is.EqualTo(navDate), "NavDate must not be overwritten");
            Assert.That(stored.Capital, Is.EqualTo(2_000_000m));
            Assert.That(stored.NumberOfOwners, Is.EqualTo(800));
            Assert.That(stored.Risk, Is.EqualTo(4));
            Assert.That(stored.SharpeRatio, Is.EqualTo(1.5m));
            Assert.That(stored.StandardDeviation, Is.EqualTo(14m));
        });
    }

    [Test]
    public async Task UpsertSparseRangeAsync_ExistingRecord_NavIsNeverOverwritten()
    {
        // Arrange
        var navDate = new DateOnly(2025, 3, 1);
        var original = CreateRecord(_testIsin, navDate, nav: 100m);
        await _sut.UpsertSparseRangeAsync([original]);
        await _sut.SaveChangesAsync();

        // Act — send a different Nav value
        var update = CreateRecord(_testIsin, navDate, nav: 9999m);
        await _sut.UpsertSparseRangeAsync([update]);
        await _sut.SaveChangesAsync();

        // Assert — Nav stays as original
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.That(stored.Nav, Is.EqualTo(100m));
    }

    [Test]
    public async Task UpsertSparseRangeAsync_ExistingRecord_WithNullSparseFields_PreservesExistingValues()
    {
        // Arrange — seed with all sparse fields set
        var navDate = new DateOnly(2025, 3, 1);
        var original = CreateRecord(_testIsin, navDate,
            nav: 100m, capital: 1_000_000m, numberOfOwners: 500, risk: 3,
            sharpeRatio: 1.0m, standardDeviation: 10m);
        await _sut.UpsertSparseRangeAsync([original]);
        await _sut.SaveChangesAsync();

        // Act — send incoming with all sparse fields null (simulates Nav-only update)
        var sparseUpdate = new FundHistoryRecord
        {
            IsinId = _testIsin,
            NavDate = navDate,
            Nav = 110m,        // different Nav — must be ignored
            Capital = null,
            NumberOfOwners = null,
            Risk = null,
            SharpeRatio = null,
            StandardDeviation = null
        };
        await _sut.UpsertSparseRangeAsync([sparseUpdate]);
        await _sut.SaveChangesAsync();

        // Assert — all original sparse field values survive; Nav unchanged too
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Nav, Is.EqualTo(100m), "Nav must not be overwritten");
            Assert.That(stored.Capital, Is.EqualTo(1_000_000m));
            Assert.That(stored.NumberOfOwners, Is.EqualTo(500));
            Assert.That(stored.Risk, Is.EqualTo(3));
            Assert.That(stored.SharpeRatio, Is.EqualTo(1.0m));
            Assert.That(stored.StandardDeviation, Is.EqualTo(10m));
        });
    }

    [Test]
    public async Task UpsertSparseRangeAsync_ExistingRecord_PartiallyFilledUpdate_OnlyUpdatesNonNullFields()
    {
        // Arrange
        var navDate = new DateOnly(2025, 6, 15);
        var original = CreateRecord(_testIsin, navDate,
            nav: 50m, capital: 500_000m, numberOfOwners: 100, risk: 2,
            sharpeRatio: 0.5m, standardDeviation: 5m);
        await _sut.UpsertSparseRangeAsync([original]);
        await _sut.SaveChangesAsync();

        // Act — only Capital and Risk are provided
        var partialUpdate = new FundHistoryRecord
        {
            IsinId = _testIsin,
            NavDate = navDate,
            Nav = null,
            Capital = 750_000m,   // update
            NumberOfOwners = null, // preserve
            Risk = 3,              // update
            SharpeRatio = null,    // preserve
            StandardDeviation = null // preserve
        };
        await _sut.UpsertSparseRangeAsync([partialUpdate]);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundHistoryRecords.FirstAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Nav, Is.EqualTo(50m));
            Assert.That(stored.Capital, Is.EqualTo(750_000m));
            Assert.That(stored.NumberOfOwners, Is.EqualTo(100));
            Assert.That(stored.Risk, Is.EqualTo(3));
            Assert.That(stored.SharpeRatio, Is.EqualTo(0.5m));
            Assert.That(stored.StandardDeviation, Is.EqualTo(5m));
        });
    }

    #endregion

    #region Mixed insert + update

    [Test]
    public async Task UpsertSparseRangeAsync_MixedNewAndExisting_InsertsNewAndUpdatesExisting()
    {
        // Arrange — pre-seed one record
        var existingDate = new DateOnly(2025, 1, 1);
        var existing = CreateRecord(_testIsin, existingDate, nav: 100m, capital: 1_000_000m);
        await _sut.UpsertSparseRangeAsync([existing]);
        await _sut.SaveChangesAsync();

        // Act — batch with an update to existing date + a new date
        var newDate = new DateOnly(2025, 1, 2);
        var batch = new[]
        {
            CreateRecord(_testIsin, existingDate, nav: 999m, capital: 2_000_000m), // update existing (Nav ignored)
            CreateRecord(_testIsin, newDate, nav: 102m, capital: 1_200_000m)        // new insert
        };
        await _sut.UpsertSparseRangeAsync(batch);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundHistoryRecords.CountAsync();
        Assert.That(count, Is.EqualTo(2));

        var updatedRecord = await _context.FundHistoryRecords
            .FirstAsync(r => r.NavDate == existingDate);
        Assert.That(updatedRecord.Nav, Is.EqualTo(100m), "Nav must not be overwritten on existing record");
        Assert.That(updatedRecord.Capital, Is.EqualTo(2_000_000m));

        var insertedRecord = await _context.FundHistoryRecords
            .FirstAsync(r => r.NavDate == newDate);
        Assert.That(insertedRecord.Nav, Is.EqualTo(102m));
        Assert.That(insertedRecord.Capital, Is.EqualTo(1_200_000m));
    }

    #endregion

    #region Helpers

    private static FundHistoryRecord CreateRecord(
        IsinId isin,
        DateOnly navDate,
        decimal? nav = null,
        decimal? capital = null,
        int? numberOfOwners = null,
        int? risk = null,
        decimal? sharpeRatio = null,
        decimal? standardDeviation = null)
    {
        return new FundHistoryRecord
        {
            IsinId = isin,
            NavDate = navDate,
            Nav = nav,
            Capital = capital,
            NumberOfOwners = numberOfOwners,
            Risk = risk,
            SharpeRatio = sharpeRatio,
            StandardDeviation = standardDeviation
        };
    }

    #endregion
}
