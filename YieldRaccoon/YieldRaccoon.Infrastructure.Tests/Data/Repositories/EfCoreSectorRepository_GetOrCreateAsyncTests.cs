using NUnit.Framework;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Data.Repositories;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;

namespace YieldRaccoon.Infrastructure.Tests.Data.Repositories;

[TestFixture]
[TestOf(typeof(EfCoreSectorRepository))]
public class EfCoreSectorRepository_GetOrCreateAsyncTests
{
    private YieldRaccoonDbContext _context = null!;
    private EfCoreSectorRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _context = InMemoryDbContextFactory.Create();
        _sut = new EfCoreSectorRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task GetOrCreateAsync_DisplayNameNotInDb_InsertsAndReturns()
    {
        var sector = await _sut.GetOrCreateAsync("Teknik");
        await _context.SaveChangesAsync();

        Assert.That(sector.DisplayName, Is.EqualTo("Teknik"));
        Assert.That(sector.Id.Value, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task GetOrCreateAsync_DisplayNameExists_ReturnsExistingWithoutInsert()
    {
        var first = await _sut.GetOrCreateAsync("Råvaror");
        await _context.SaveChangesAsync();

        var second = await _sut.GetOrCreateAsync("Råvaror");

        Assert.That(second.Id, Is.EqualTo(first.Id));
        Assert.That(_context.Sectors.Local.Count, Is.EqualTo(1));
    }
}
