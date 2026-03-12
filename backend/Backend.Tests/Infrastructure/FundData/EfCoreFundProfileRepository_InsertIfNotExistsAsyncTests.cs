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
public class EfCoreFundProfileRepository_InsertIfNotExistsAsyncTests
{
    private FundDataDbContext _context = null!;
    private EfCoreFundProfileRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _context = InMemoryFundDataDbContextFactory.Create();
        _sut = new EfCoreFundProfileRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    #region Insert (profile absent)

    [Test]
    public async Task InsertIfNotExistsAsync_NewProfile_InsertsIntoDatabase()
    {
        // Arrange
        var profile = CreateProfile("SE0008613939", "Test Fund A");

        // Act
        await _sut.InsertIfNotExistsAsync(profile);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundProfiles.FirstOrDefaultAsync();
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Name, Is.EqualTo("Test Fund A"));
        Assert.That(stored.Id.Isin, Is.EqualTo("SE0008613939"));
    }

    [Test]
    public async Task InsertIfNotExistsAsync_NewProfile_PreservesAllFields()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var profile = new FundProfile
        {
            Id = IsinId.Create("SE0008613939"),
            Name = "Full Fields Fund",
            FirstSeenAt = now,
            CrawlerLastUpdatedAt = now,
            Category = "Equity",
            CompanyName = "Test Company",
            FundType = "Aktiefond",
            Rating = 5,
            ManagementFee = 0.0125m,
            EsgScore = 22.5m,
            Buyable = true
        };

        // Act
        await _sut.InsertIfNotExistsAsync(profile);
        await _sut.SaveChangesAsync();

        // Assert
        var stored = await _context.FundProfiles.FindAsync(IsinId.Create("SE0008613939"));
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Category, Is.EqualTo("Equity"));
            Assert.That(stored.CompanyName, Is.EqualTo("Test Company"));
            Assert.That(stored.Rating, Is.EqualTo(5));
            Assert.That(stored.ManagementFee, Is.EqualTo(0.0125m));
            Assert.That(stored.EsgScore, Is.EqualTo(22.5m));
            Assert.That(stored.Buyable, Is.True);
        });
    }

    #endregion

    #region Skip (profile already exists)

    [Test]
    public async Task InsertIfNotExistsAsync_ExistingProfile_DoesNotInsertDuplicate()
    {
        // Arrange — insert original
        var profile = CreateProfile("SE0008613939", "Original Fund");
        await _sut.InsertIfNotExistsAsync(profile);
        await _sut.SaveChangesAsync();

        // Act — attempt to insert again
        var duplicate = CreateProfile("SE0008613939", "Duplicate Fund");
        await _sut.InsertIfNotExistsAsync(duplicate);
        await _sut.SaveChangesAsync();

        // Assert — still exactly one record
        var count = await _context.FundProfiles.CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task InsertIfNotExistsAsync_ExistingProfile_DoesNotModifyExistingFields()
    {
        // Arrange — insert original with specific values
        var isinId = IsinId.Create("SE0008613939");
        var original = new FundProfile
        {
            Id = isinId,
            Name = "Original Name",
            FirstSeenAt = DateTimeOffset.UtcNow,
            Category = "Equity",
            CompanyName = "Original Company",
            Rating = 5
        };
        await _sut.InsertIfNotExistsAsync(original);
        await _sut.SaveChangesAsync();

        // Act — attempt insert with different values
        var incoming = new FundProfile
        {
            Id = isinId,
            Name = "New Name",
            FirstSeenAt = DateTimeOffset.UtcNow,
            Category = "Bond",
            CompanyName = "New Company",
            Rating = 2
        };
        await _sut.InsertIfNotExistsAsync(incoming);
        await _sut.SaveChangesAsync();

        // Assert — all original values unchanged
        var stored = await _context.FundProfiles.FindAsync(isinId);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Name, Is.EqualTo("Original Name"));
            Assert.That(stored.Category, Is.EqualTo("Equity"));
            Assert.That(stored.CompanyName, Is.EqualTo("Original Company"));
            Assert.That(stored.Rating, Is.EqualTo(5));
        });
    }

    #endregion

    #region Multiple profiles

    [Test]
    public async Task InsertIfNotExistsAsync_MultipleDifferentProfiles_InsertsAll()
    {
        // Arrange
        var profileA = CreateProfile("SE0008613939", "Fund A");
        var profileB = CreateProfile("LU0274208692", "Fund B");

        // Act
        await _sut.InsertIfNotExistsAsync(profileA);
        await _sut.InsertIfNotExistsAsync(profileB);
        await _sut.SaveChangesAsync();

        // Assert
        var count = await _context.FundProfiles.CountAsync();
        Assert.That(count, Is.EqualTo(2));
    }

    #endregion

    #region Helpers

    private static FundProfile CreateProfile(string isin, string name)
    {
        return new FundProfile
        {
            Id = IsinId.Create(isin),
            Name = name,
            FirstSeenAt = DateTimeOffset.UtcNow,
            CrawlerLastUpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
