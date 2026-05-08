using NUnit.Framework;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Data.Repositories;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;

namespace YieldRaccoon.Infrastructure.Tests.Data.Repositories;

[TestFixture]
[TestOf(typeof(EfCoreCountryRepository))]
public class EfCoreCountryRepository_GetOrCreateAsyncTests
{
    private YieldRaccoonDbContext _context = null!;
    private EfCoreCountryRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _context = InMemoryDbContextFactory.Create();
        _sut = new EfCoreCountryRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task GetOrCreateAsync_DisplayNameNotInDb_InsertsAndReturns()
    {
        var country = await _sut.GetOrCreateAsync("USA", "US");
        await _context.SaveChangesAsync();

        Assert.That(country.DisplayName, Is.EqualTo("USA"));
        Assert.That(country.CountryCode, Is.EqualTo("US"));
        Assert.That(country.Id.Value, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task GetOrCreateAsync_DisplayNameExists_ReturnsExistingWithoutInsert()
    {
        var first = await _sut.GetOrCreateAsync("Sverige", "SE");
        await _context.SaveChangesAsync();

        var second = await _sut.GetOrCreateAsync("Sverige", "SE");

        Assert.That(second.Id, Is.EqualTo(first.Id));
        Assert.That(_context.Countries.Local.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetOrCreateAsync_ExistingHasNullCode_BackfillsWhenPayloadHasCode()
    {
        var first = await _sut.GetOrCreateAsync("USA", null);
        await _context.SaveChangesAsync();
        Assert.That(first.CountryCode, Is.Null);

        var second = await _sut.GetOrCreateAsync("USA", "US");
        await _context.SaveChangesAsync();

        Assert.That(second.Id, Is.EqualTo(first.Id));
        Assert.That(second.CountryCode, Is.EqualTo("US"));
    }

    [Test]
    public async Task GetOrCreateAsync_ExistingHasCode_DoesNotOverwriteWithNull()
    {
        var first = await _sut.GetOrCreateAsync("USA", "US");
        await _context.SaveChangesAsync();

        var second = await _sut.GetOrCreateAsync("USA", null);
        await _context.SaveChangesAsync();

        Assert.That(second.CountryCode, Is.EqualTo("US"),
            "Existing non-null code must not be overwritten with null on re-encounter");
    }
}
