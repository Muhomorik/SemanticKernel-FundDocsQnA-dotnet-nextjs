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

    /// <summary>
    /// IDbContextFactory that creates InMemory contexts with a shared database name.
    /// </summary>
    private sealed class InMemoryDbContextFactory(string dbName) : IDbContextFactory<FundDataDbContext>
    {
        public FundDataDbContext CreateDbContext() => InMemoryFundDataDbContextFactory.Create(dbName);
    }
}
