using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Data.Repositories;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;

namespace YieldRaccoon.Infrastructure.Tests.Data.Repositories;

/// <summary>
/// Tests for <see cref="EfCoreFundProfileRepository.GetIsinByOrderBookIdAsync"/>.
/// </summary>
[TestFixture]
[TestOf(typeof(EfCoreFundProfileRepository))]
public class EfCoreFundProfileRepository_GetIsinByOrderBookIdAsyncTests
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
    public async Task GetIsinByOrderBookIdAsync_ExistingProfile_ReturnsIsin()
    {
        // Arrange
        var profile = _fixture.Create<FundProfile>();
        var orderBookId = _fixture.Create<OrderBookId>();
        profile.OrderbookId = orderBookId.Value;

        await _sut.AddOrUpdateAsync(profile);
        await _sut.SaveChangesAsync();

        // Act
        var result = await _sut.GetIsinByOrderBookIdAsync(orderBookId);

        // Assert
        Assert.That(result, Is.EqualTo(profile.Id.Isin));
    }

    [Test]
    public async Task GetIsinByOrderBookIdAsync_NonExistentOrderBookId_ReturnsNull()
    {
        // Arrange — seed a profile with a different OrderBookId
        var profile = _fixture.Create<FundProfile>();
        profile.OrderbookId = "OTHER-ID";

        await _sut.AddOrUpdateAsync(profile);
        await _sut.SaveChangesAsync();

        // Act
        var result = await _sut.GetIsinByOrderBookIdAsync(OrderBookId.Create("NONEXISTENT"));

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetIsinByOrderBookIdAsync_ProfileWithNullOrderBookId_ReturnsNull()
    {
        // Arrange — seed a profile without an OrderBookId
        var profile = _fixture.Create<FundProfile>();
        profile.OrderbookId = null;

        await _sut.AddOrUpdateAsync(profile);
        await _sut.SaveChangesAsync();

        // Act
        var result = await _sut.GetIsinByOrderBookIdAsync(_fixture.Create<OrderBookId>());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetIsinByOrderBookIdAsync_MultipleProfiles_ReturnsCorrectIsin()
    {
        // Arrange
        var targetOrderBookId = OrderBookId.Create("TARGET-001");

        var profile1 = _fixture.Create<FundProfile>();
        profile1.OrderbookId = "OTHER-001";
        var profile2 = _fixture.Create<FundProfile>();
        profile2.OrderbookId = targetOrderBookId.Value;
        var profile3 = _fixture.Create<FundProfile>();
        profile3.OrderbookId = "OTHER-002";

        await _sut.AddOrUpdateAsync(profile1);
        await _sut.AddOrUpdateAsync(profile2);
        await _sut.AddOrUpdateAsync(profile3);
        await _sut.SaveChangesAsync();

        // Act
        var result = await _sut.GetIsinByOrderBookIdAsync(targetOrderBookId);

        // Assert
        Assert.That(result, Is.EqualTo(profile2.Id.Isin));
    }
}
