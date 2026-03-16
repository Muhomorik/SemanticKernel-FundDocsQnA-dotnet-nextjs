using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Backend.API.Infrastructure.FundData;
using Backend.API.Infrastructure.FundData.Services;

using Backend.Tests.TestInfrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Infrastructure.FundData;

[TestFixture]
[Category("Unit")]
[Category("FundData.Database")]
public class OwnershipFlowService_GetOwnershipFlowAsyncTests
{
    private string _dbName = null!;
    private IMemoryCache _cache = null!;
    private OwnershipFlowService _sut = null!;

    // Test period
    private static readonly DateOnly From = new(2025, 2, 10);
    private static readonly DateOnly To = new(2025, 2, 16);

    [SetUp]
    public void SetUp()
    {
        _dbName = Guid.NewGuid().ToString();
        _cache = new MemoryCache(new MemoryCacheOptions());
        var factory = new InMemoryDbContextFactory(_dbName);
        _sut = new OwnershipFlowService(factory, _cache, NullLogger<OwnershipFlowService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
    }

    [Test]
    public async Task GetOwnershipFlowAsync_NoData_ReturnsEmptyArrays()
    {
        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.Out, Is.Empty);
        Assert.That(result.Fund.In, Is.Empty);
        Assert.That(result.Cat.Out, Is.Empty);
        Assert.That(result.Cat.In, Is.Empty);
    }

    [Test]
    public async Task GetOwnershipFlowAsync_ReturnsCorrectPeriodLabel()
    {
        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.PeriodLabel, Is.EqualTo("Feb 10 – 16"));
    }

    [Test]
    public async Task GetOwnershipFlowAsync_FundGainingOwners_AppearsInFundIn()
    {
        await SeedFund("SE0008613939", "Avanza Zero", "Aktiefond Global", numberOfOwners: 1000,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 1000),
                (new DateOnly(2025, 2, 16), 1200),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.In, Has.Count.EqualTo(1));
        Assert.That(result.Fund.In[0].Name, Is.EqualTo("Avanza Zero"));
        Assert.That(result.Fund.In[0].Value, Is.EqualTo(200));
        Assert.That(result.Fund.In[0].Pct, Is.EqualTo(20.0));
    }

    [Test]
    public async Task GetOwnershipFlowAsync_FundLosingOwners_AppearsInFundOut()
    {
        await SeedFund("SE0008613939", "SEB Sverige", "Aktiefond Sverige", numberOfOwners: 500,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 500),
                (new DateOnly(2025, 2, 16), 400),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.Out, Has.Count.EqualTo(1));
        Assert.That(result.Fund.Out[0].Name, Is.EqualTo("SEB Sverige"));
        Assert.That(result.Fund.Out[0].Value, Is.EqualTo(100)); // absolute
        Assert.That(result.Fund.Out[0].Pct, Is.EqualTo(-20.0)); // signed
    }

    [Test]
    public async Task GetOwnershipFlowAsync_FundWithZeroDelta_IsExcluded()
    {
        await SeedFund("SE0008613939", "No Change Fund", "Aktiefond Global", numberOfOwners: 500,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 500),
                (new DateOnly(2025, 2, 16), 500),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.Out, Is.Empty);
        Assert.That(result.Fund.In, Is.Empty);
    }

    [Test]
    public async Task GetOwnershipFlowAsync_FundWithOneRecord_IsExcluded()
    {
        await SeedFund("SE0008613939", "Single Record Fund", "Aktiefond Global", numberOfOwners: 500,
            historyRecords:
            [
                (new DateOnly(2025, 2, 12), 500),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.Out, Is.Empty);
        Assert.That(result.Fund.In, Is.Empty);
    }

    [Test]
    public async Task GetOwnershipFlowAsync_FundUnder100Owners_IsExcluded()
    {
        await SeedFund("SE0008613939", "Small Fund", "Aktiefond Global", numberOfOwners: 50,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 50),
                (new DateOnly(2025, 2, 16), 80),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.In, Is.Empty);
    }

    [Test]
    public async Task GetOwnershipFlowAsync_CategoryAggregation_SumsDeltas()
    {
        // Two "Sverige" funds, both losing owners
        await SeedFund("SE0008613939", "Fund A", "Aktiefond Sverige", numberOfOwners: 1000,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 1000),
                (new DateOnly(2025, 2, 16), 900),
            ]);

        await SeedFund("LU0274208692", "Fund B", "Indexfond Sverige", numberOfOwners: 2000,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 2000),
                (new DateOnly(2025, 2, 16), 1800),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        // Category "Sverige" should aggregate: -100 + -200 = -300
        Assert.That(result.Cat.Out, Has.Count.EqualTo(1));
        Assert.That(result.Cat.Out[0].Name, Is.EqualTo("Sverige"));
        Assert.That(result.Cat.Out[0].Value, Is.EqualTo(300));
    }

    [Test]
    public async Task GetOwnershipFlowAsync_MixedDirections_SplitsCorrectly()
    {
        await SeedFund("SE0008613939", "Losing Fund", "Aktiefond Sverige", numberOfOwners: 1000,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 1000),
                (new DateOnly(2025, 2, 16), 800),
            ]);

        await SeedFund("LU0274208692", "Gaining Fund", "Aktiefond Global", numberOfOwners: 500,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 500),
                (new DateOnly(2025, 2, 16), 700),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.Out, Has.Count.EqualTo(1));
        Assert.That(result.Fund.Out[0].Name, Is.EqualTo("Losing Fund"));
        Assert.That(result.Fund.In, Has.Count.EqualTo(1));
        Assert.That(result.Fund.In[0].Name, Is.EqualTo("Gaining Fund"));
    }

    [Test]
    public async Task GetOwnershipFlowAsync_Top10Limit_RespectsLimit()
    {
        // Seed 12 gaining funds — should only return top 10
        for (var i = 0; i < 12; i++)
        {
            var isin = $"SE{i:D10}";
            await SeedFund(isin, $"Fund {i}", "Aktiefond Global", numberOfOwners: 1000,
                historyRecords:
                [
                    (new DateOnly(2025, 2, 10), 1000),
                    (new DateOnly(2025, 2, 16), 1000 + (i + 1) * 100),
                ]);
        }

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.In, Has.Count.EqualTo(10));
        // Should be sorted by delta descending — Fund 11 (delta=1200) should be first
        Assert.That(result.Fund.In[0].Value, Is.EqualTo(1200));
    }

    [Test]
    public async Task GetOwnershipFlowAsync_FundOutsideDateRange_IsExcluded()
    {
        await SeedFund("SE0008613939", "Outside Fund", "Aktiefond Global", numberOfOwners: 1000,
            historyRecords:
            [
                (new DateOnly(2025, 2, 1), 1000),  // before range
                (new DateOnly(2025, 2, 8), 1200),  // before range
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.In, Is.Empty);
    }

    [Test]
    public async Task GetOwnershipFlowAsync_CachesResult()
    {
        await SeedFund("SE0008613939", "Cached Fund", "Aktiefond Global", numberOfOwners: 1000,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 1000),
                (new DateOnly(2025, 2, 16), 1200),
            ]);

        var result1 = await _sut.GetOwnershipFlowAsync(From, To);
        var result2 = await _sut.GetOwnershipFlowAsync(From, To);

        // Same reference means cache hit
        Assert.That(result2, Is.SameAs(result1));
    }

    [Test]
    public async Task GetOwnershipFlowAsync_StartOwnersZero_PctIsZero()
    {
        await SeedFund("SE0008613939", "Zero Start Fund", "Aktiefond Global", numberOfOwners: 500,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 0),
                (new DateOnly(2025, 2, 16), 100),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Fund.In, Has.Count.EqualTo(1));
        Assert.That(result.Fund.In[0].Pct, Is.EqualTo(0.0));
    }

    [Test]
    public async Task GetOwnershipFlowAsync_NullCategory_MapsToOther()
    {
        await SeedFund("SE0008613939", "No Cat Fund", category: null, numberOfOwners: 1000,
            historyRecords:
            [
                (new DateOnly(2025, 2, 10), 1000),
                (new DateOnly(2025, 2, 16), 1200),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To);

        Assert.That(result.Cat.In, Has.Count.EqualTo(1));
        Assert.That(result.Cat.In[0].Name, Is.EqualTo("Other"));
    }

    // ─── Look-back bug: monthly periods return identical data (production bug) ───
    //
    // Bug: query used NavDate >= from AND NavDate <= to.
    // A fund with its earliest record at Feb 14 appears identical for "1 month"
    // (Feb 11–Mar 11), "2 months" (Jan 11–Mar 11), and "3 months" (Dec 11–Mar 11),
    // because all three periods find the same Feb 14 record as the start point.
    //
    // Fix: use the most recent snapshot at-or-before `from` as the baseline,
    // so longer periods look further back and produce genuinely different deltas.

    [Test]
    public async Task LookBack_RecordBeforeFromDate_UsedAsStartBaseline()
    {
        // Fund has a snapshot BEFORE the period start, and one inside — currently
        // the service misses the prior record and sees only 1 in-range record → empty.
        // After the fix: start = Jan 15 (most recent before Feb 10), end = Feb 14, delta = +300.
        await SeedFund("SE0008613939", "Look-back Fund", "Aktiefond Global", numberOfOwners: 1000,
            historyRecords:
            [
                (new DateOnly(2025, 1, 15), 500),  // before period start — should be baseline
                (new DateOnly(2025, 2, 14), 800),  // inside period
            ]);

        var result = await _sut.GetOwnershipFlowAsync(From, To); // Feb 10 – 16

        Assert.That(result.Fund.In, Has.Count.EqualTo(1));
        Assert.That(result.Fund.In[0].Value, Is.EqualTo(300)); // 800 - 500
    }

    [Test]
    public async Task LookBack_DifferentFromDates_ProduceDifferentDeltas()
    {
        // This is the core production bug: 1-month and 3-month periods return
        // identical flow data because they find the same "earliest in-range" record.
        //
        // Fund snapshots: Oct 10 (1000), Jan 15 (1500), Mar 7 (1800)
        //   "1 month"  Feb 11 – Mar 11: start should be Jan 15 (1500), end = Mar 7 (1800) → delta +300
        //   "3 months" Dec 11 – Mar 11: start should be Oct 10 (1000), end = Mar 7 (1800) → delta +800
        //
        // Buggy behavior: both periods find Jan 15 as earliest in-range record → both delta = +300.

        await SeedFund("SE0008613939", "Multi-Period Fund", "Aktiefond Global", numberOfOwners: 2000,
            historyRecords:
            [
                (new DateOnly(2024, 10, 10), 1000), // only within "3 months" look-back
                (new DateOnly(2025, 1, 15),  1500), // within both "1 month" and "3 months"
                (new DateOnly(2025, 3, 7),   1800), // the common end point
            ]);

        var oneMonth   = await _sut.GetOwnershipFlowAsync(
            new DateOnly(2025, 2, 11), new DateOnly(2025, 3, 11));

        var threeMonths = await _sut.GetOwnershipFlowAsync(
            new DateOnly(2024, 12, 11), new DateOnly(2025, 3, 11));

        // 1 month: baseline = Jan 15 (1500), end = Mar 7 (1800)
        Assert.That(oneMonth.Fund.In, Has.Count.EqualTo(1));
        Assert.That(oneMonth.Fund.In[0].Value, Is.EqualTo(300));

        // 3 months: baseline = Oct 10 (1000), end = Mar 7 (1800) — genuinely different!
        Assert.That(threeMonths.Fund.In, Has.Count.EqualTo(1));
        Assert.That(threeMonths.Fund.In[0].Value, Is.EqualTo(800));
    }

    // ─── Monday edge case: from == to (regression test) ────────────────────────
    //
    // On Monday, BuildWeeklyPeriods creates a current week with from == to.
    // The service must handle this gracefully — return valid data, not throw.

    [Test]
    public async Task GetOwnershipFlowAsync_FromEqualsTo_NoData_DoesNotThrow()
    {
        var monday = new DateOnly(2026, 3, 16);

        var result = await _sut.GetOwnershipFlowAsync(monday, monday);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.PeriodLabel, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetOwnershipFlowAsync_FromEqualsTo_WithData_DoesNotThrow()
    {
        var monday = new DateOnly(2026, 3, 16);

        await SeedFund("SE0008613939", "Monday Fund", "Aktiefond Global", numberOfOwners: 1000,
            historyRecords:
            [
                (new DateOnly(2026, 3, 16), 1000),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(monday, monday);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.PeriodLabel, Is.Not.Null.And.Not.Empty);
    }

    // ─── Weekly period with sparse NumberOfOwners (production scenario) ─────────
    //
    // In production, NumberOfOwners is updated infrequently. A 7-day window often
    // contains 0 or 1 snapshots per fund, so the service returns empty arrays.
    // (Observed in logs: Feb 16–22 2026 → 0 outflows, 0 inflows.)

    private static readonly DateOnly WeekFrom = new(2026, 2, 16); // Mon
    private static readonly DateOnly WeekTo = new(2026, 2, 22);   // Sun

    [Test]
    public async Task WeeklyPeriod_ReturnsCorrectPeriodLabel()
    {
        var result = await _sut.GetOwnershipFlowAsync(WeekFrom, WeekTo);

        Assert.That(result.PeriodLabel, Is.EqualTo("Feb 16 – 22"));
    }

    [Test]
    public async Task WeeklyPeriod_AllHistoryRecordsHaveNullOwners_ReturnsEmpty()
    {
        // Owners are not updated this week — records exist but NumberOfOwners is null.
        // Query 1 filters them out, leaving no qualifying records.
        await SeedFundNullable("SE0008613939", "Sparse Fund", "Aktiefond Sverige",
            profileOwners: 5000,
            historyRecords:
            [
                (new DateOnly(2026, 2, 16), null),
                (new DateOnly(2026, 2, 19), null),
                (new DateOnly(2026, 2, 22), null),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(WeekFrom, WeekTo);

        Assert.That(result.Fund.Out, Is.Empty);
        Assert.That(result.Fund.In, Is.Empty);
        Assert.That(result.Cat.Out, Is.Empty);
        Assert.That(result.Cat.In, Is.Empty);
    }

    [Test]
    public async Task WeeklyPeriod_OnlyOneRecordHasOwners_IsExcluded()
    {
        // Delta requires at least 2 records with non-null owners in the range.
        // One snapshot mid-week is not enough.
        await SeedFundNullable("SE0008613939", "Single Snapshot Fund", "Aktiefond Sverige",
            profileOwners: 5000,
            historyRecords:
            [
                (new DateOnly(2026, 2, 16), null),
                (new DateOnly(2026, 2, 19), 5100), // only one non-null record
                (new DateOnly(2026, 2, 22), null),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(WeekFrom, WeekTo);

        Assert.That(result.Fund.Out, Is.Empty);
        Assert.That(result.Fund.In, Is.Empty);
    }

    [Test]
    public async Task WeeklyPeriod_TwoRecordsWithOwners_ComputesDeltaCorrectly()
    {
        // When owners snapshots exist at both ends of the weekly window, delta is computed.
        await SeedFundNullable("SE0008613939", "Active Fund", "Aktiefond Sverige",
            profileOwners: 5000,
            historyRecords:
            [
                (new DateOnly(2026, 2, 16), 5000),
                (new DateOnly(2026, 2, 22), 5300),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(WeekFrom, WeekTo);

        Assert.That(result.Fund.In, Has.Count.EqualTo(1));
        Assert.That(result.Fund.In[0].Value, Is.EqualTo(300));
        Assert.That(result.Fund.In[0].Pct, Is.EqualTo(6.0));
    }

    [Test]
    public async Task WeeklyPeriod_OwnersDataOnlyBeforeRange_IsExcluded()
    {
        // Owners snapshots from the preceding week are outside the date filter — ignored.
        await SeedFundNullable("SE0008613939", "Prior Week Fund", "Aktiefond Sverige",
            profileOwners: 5000,
            historyRecords:
            [
                (new DateOnly(2026, 2, 9),  4800), // prior week — excluded by date filter
                (new DateOnly(2026, 2, 15), 5000), // day before range — excluded by date filter
                (new DateOnly(2026, 2, 17), null), // in range, null → excluded by null filter
                (new DateOnly(2026, 2, 22), null), // in range, null → excluded by null filter
            ]);

        var result = await _sut.GetOwnershipFlowAsync(WeekFrom, WeekTo);

        Assert.That(result.Fund.Out, Is.Empty);
        Assert.That(result.Fund.In, Is.Empty);
    }

    [Test]
    public async Task WeeklyPeriod_MultipleNonNullRecords_UsesBoundaryValues()
    {
        // Service uses first and last records by NavDate, not any midpoint.
        // Three owner snapshots this week: 5000 → 5050 → 5200.
        // Expected delta: 5200 - 5000 = 200.
        await SeedFundNullable("SE0008613939", "Multi Snapshot Fund", "Aktiefond Sverige",
            profileOwners: 5000,
            historyRecords:
            [
                (new DateOnly(2026, 2, 16), 5000),
                (new DateOnly(2026, 2, 18), 5050), // intermediate — not used
                (new DateOnly(2026, 2, 22), 5200),
            ]);

        var result = await _sut.GetOwnershipFlowAsync(WeekFrom, WeekTo);

        Assert.That(result.Fund.In, Has.Count.EqualTo(1));
        Assert.That(result.Fund.In[0].Value, Is.EqualTo(200)); // 5200 - 5000
    }

    // ─── Seed helpers ───────────────────────────────────────────────────────────

    private async Task SeedFund(
        string isin, string name, string? category, int numberOfOwners,
        (DateOnly date, int owners)[] historyRecords)
    {
        await using var context = InMemoryFundDataDbContextFactory.Create(_dbName);

        // Only add profile if it doesn't exist yet
        var isinId = IsinId.Create(isin);
        var existing = await context.FundProfiles.FindAsync(isinId);
        if (existing is null)
        {
            context.FundProfiles.Add(new FundProfile
            {
                Id = isinId,
                Name = name,
                Category = category,
                NumberOfOwners = numberOfOwners,
                FirstSeenAt = DateTimeOffset.UtcNow,
            });
        }

        foreach (var (date, owners) in historyRecords)
        {
            context.FundHistoryRecords.Add(new FundHistoryRecord
            {
                IsinId = isinId,
                NavDate = date,
                NumberOfOwners = owners,
            });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Variant of SeedFund that accepts nullable owners in history records.</summary>
    private async Task SeedFundNullable(
        string isin, string name, string? category, int profileOwners,
        (DateOnly date, int? owners)[] historyRecords)
    {
        await using var context = InMemoryFundDataDbContextFactory.Create(_dbName);

        var isinId = IsinId.Create(isin);
        var existing = await context.FundProfiles.FindAsync(isinId);
        if (existing is null)
        {
            context.FundProfiles.Add(new FundProfile
            {
                Id = isinId,
                Name = name,
                Category = category,
                NumberOfOwners = profileOwners,
                FirstSeenAt = DateTimeOffset.UtcNow,
            });
        }

        foreach (var (date, owners) in historyRecords)
        {
            context.FundHistoryRecords.Add(new FundHistoryRecord
            {
                IsinId = isinId,
                NavDate = date,
                NumberOfOwners = owners,
            });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// IDbContextFactory that creates InMemory contexts with a shared database name.
    /// </summary>
    private sealed class InMemoryDbContextFactory(string dbName) : IDbContextFactory<FundDataDbContext>
    {
        public FundDataDbContext CreateDbContext() => InMemoryFundDataDbContextFactory.Create(dbName);
    }
}
