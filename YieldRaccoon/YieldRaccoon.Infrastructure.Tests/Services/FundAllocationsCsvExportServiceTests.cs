using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(FundAllocationsCsvExportService))]
public class FundAllocationsCsvExportServiceTests
{
    private const string FundAIsin = "SE0000000011"; // Buyable, TestCo, 500 owners — has both kinds
    private const string FundBIsin = "SE0000000022"; // Buyable, OtherCo, 100 owners — sectors only
    private const string FundCIsin = "SE0000000033"; // Buyable, TestCo, 200 owners — no allocations at all
    private const string FundDIsin = "SE0000000044"; // NON-buyable, TestCo, 300 owners — has allocations
    private const string FundEIsin = "SE0000000055"; // Buyable, TestCo, 10 owners — below min-owners threshold

    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private FundAllocationsCsvExportService _sut = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _sut = _fixture.Create<FundAllocationsCsvExportService>();

        _tempDir = Path.Combine(Path.GetTempPath(), $"YieldRaccoon_AllocTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region Header & schema

    [Test]
    public async Task ExportAsync_HeaderRow_OrdersCountriesBeforeSectorsAndAlphaSortsEachBlock()
    {
        // Arrange — three countries, two sectors. Sanitized: country_storbritannien, country_sverige,
        // country_usa, sector_industri, sector_teknik. Alphabetical order within each block.
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert
        var lines = await File.ReadAllLinesAsync(destPath);
        Assert.That(lines[0], Is.EqualTo(
            "isin,name,country_storbritannien,country_sverige,country_usa,sector_industri,sector_teknik"));
    }

    [Test]
    public async Task ExportAsync_FundWithBothCountryAndSectorAllocations_EmitsValuesAndZeroFillsAbsentColumns()
    {
        // Arrange — Fund A holds Sweden 60.5%, USA 30%; Industri 50%. UK / Teknik are absent → 0.
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert
        var lines = await File.ReadAllLinesAsync(destPath);
        var fundARow = lines.Single(l => l.StartsWith(FundAIsin));
        Assert.That(fundARow, Is.EqualTo($"{FundAIsin},Fund A,0,60.5,30.0,50.0,0"));
    }

    [Test]
    public async Task ExportAsync_FundWithSectorAllocationsOnly_StillIncluded_AndZeroFillsAllCountryColumns()
    {
        // Arrange — Fund B has only Teknik 100% and no country rows. Must NOT be skipped.
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert
        var lines = await File.ReadAllLinesAsync(destPath);
        var fundBRow = lines.Single(l => l.StartsWith(FundBIsin));
        Assert.That(fundBRow, Is.EqualTo($"{FundBIsin},Fund B,0,0,0,0,100.0"));
    }

    #endregion

    #region Filters

    [Test]
    public async Task ExportAsync_FundWithNoAllocationsInEitherTable_IsExcludedEntirely()
    {
        // Arrange — Fund C is buyable and meets min-owners but has zero allocation rows.
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Not.Contain(FundCIsin),
            "Funds with no allocations in either table must be excluded entirely.");
    }

    [Test]
    public async Task ExportAsync_NonBuyableFund_IsExcludedEvenWhenItHasAllocations()
    {
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        await _sut.ExportAsync(sourcePath, destPath);

        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Not.Contain(FundDIsin), "Non-buyable funds must be excluded.");
    }

    [Test]
    public async Task ExportAsync_MinOwnersFilter_ExcludesFundsBelowThreshold()
    {
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        await _sut.ExportAsync(sourcePath, destPath, minNumberOfOwners: 100);

        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Not.Contain(FundEIsin), "Fund E (10 owners) must be excluded.");
        Assert.That(content, Does.Contain(FundAIsin), "Fund A (500 owners) must be included.");
    }

    [Test]
    public async Task ExportAsync_CompanyFilter_KeepsOnlyMatchingCompany()
    {
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        await _sut.ExportAsync(sourcePath, destPath, companyName: "OtherCo");

        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Contain(FundBIsin));
        Assert.That(content, Does.Not.Contain(FundAIsin), "TestCo funds must be excluded when filter=OtherCo.");
    }

    [Test]
    public async Task ExportAsync_RowCount_OnlyCountsIncludedFunds()
    {
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        // Act — default args (no company filter, minOwners=0). Excluded: Fund C (no allocs), Fund D (non-buyable).
        // Fund E has 10 owners but minOwners=0 keeps it. Fund E has allocations seeded? No — seeding helper
        // gives Fund E zero allocations. So Fund E is also excluded by the no-allocations rule.
        var rowCount = await _sut.ExportAsync(sourcePath, destPath);

        // Assert — only Fund A and Fund B
        Assert.That(rowCount, Is.EqualTo(2));
    }

    #endregion

    #region Sanitization & escaping

    [Test]
    public async Task ExportAsync_HeaderColumns_AreSanitizedAsciiOnly()
    {
        // Arrange — seed a country with diacritics ("Sverige"→"sverige") and a sector with diacritics
        // ("Råvaror"→"ravaror").
        var sourcePath = await CreateSourceDatabaseWithDiacriticsAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert
        var lines = await File.ReadAllLinesAsync(destPath);
        Assert.That(lines[0], Is.EqualTo("isin,name,country_sverige,sector_ravaror"));
    }

    [Test]
    public async Task ExportAsync_FundNameWithComma_IsRfc4180Escaped()
    {
        // Arrange — seed a fund whose name contains a comma; it must be wrapped in double quotes.
        var sourcePath = await CreateSourceDatabaseWithCommaNameAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Contain("\"Acme Fund, Class A\""));
    }

    [Test]
    public async Task ExportAsync_TwoCountriesSanitizingToTheSameColumn_Throws()
    {
        // Arrange — seed two countries that ASCII-fold to the same suffix. Real-world example:
        // pair "Curaçao" with a hypothetical "Curacao" — both → "curacao".
        var sourcePath = await CreateSourceDatabaseWithCollisionAsync();
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        // Act + Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExportAsync(sourcePath, destPath));
        Assert.That(ex!.Message, Does.Contain("country_curacao"));
        Assert.That(ex.Message, Does.Contain("Curaçao"));
        Assert.That(ex.Message, Does.Contain("Curacao"));
    }

    #endregion

    #region Error handling

    [Test]
    public void ExportAsync_SourceNotFound_ThrowsFileNotFoundException()
    {
        var fakePath = Path.Combine(_tempDir, "nonexistent.db");
        var destPath = Path.Combine(_tempDir, "allocations.csv");

        Assert.ThrowsAsync<FileNotFoundException>(() => _sut.ExportAsync(fakePath, destPath));
    }

    [Test]
    public async Task ExportAsync_CreatesOutputDirectoryIfMissing()
    {
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "subdir", "nested", "allocations.csv");

        await _sut.ExportAsync(sourcePath, destPath);

        Assert.That(File.Exists(destPath), Is.True);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Seeds the canonical test database used by the bulk of the tests.
    /// 5 funds (A/B/C/D/E), 3 countries (Sverige/USA/Storbritannien — UK has no allocations to
    /// exercise the all-zero-column case), 2 sectors (Industri/Teknik).
    /// </summary>
    private async Task<string> CreateSourceDatabaseAsync()
    {
        var dbPath = Path.Combine(_tempDir, "source.db");
        await using var context = OpenContext(dbPath);
        await context.Database.EnsureCreatedAsync();

        var now = DateTimeOffset.UtcNow;

        var sverige = new Country { Id = CountryId.New(), DisplayName = "Sverige", CountryCode = "SE" };
        var usa = new Country { Id = CountryId.New(), DisplayName = "USA", CountryCode = "US" };
        var uk = new Country { Id = CountryId.New(), DisplayName = "Storbritannien", CountryCode = "GB" };
        context.Countries.AddRange(sverige, usa, uk);

        var industri = new Sector { Id = SectorId.New(), DisplayName = "Industri" };
        var teknik = new Sector { Id = SectorId.New(), DisplayName = "Teknik" };
        context.Sectors.AddRange(industri, teknik);

        // Fund A — buyable, TestCo, 500 owners, both kinds of allocations
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create(FundAIsin),
            Name = "Fund A",
            CompanyName = "TestCo",
            NumberOfOwners = 500,
            Buyable = true,
            FirstSeenAt = now
        });

        context.FundCountryAllocations.AddRange(
            new FundCountryAllocation
            {
                Id = FundCountryAllocationId.New(),
                IsinId = IsinId.Create(FundAIsin),
                CountryId = sverige.Id,
                Percentage = 60.5m
            },
            new FundCountryAllocation
            {
                Id = FundCountryAllocationId.New(),
                IsinId = IsinId.Create(FundAIsin),
                CountryId = usa.Id,
                Percentage = 30.0m
            });

        context.FundSectorAllocations.Add(new FundSectorAllocation
        {
            Id = FundSectorAllocationId.New(),
            IsinId = IsinId.Create(FundAIsin),
            SectorId = industri.Id,
            Percentage = 50.0m
        });

        // Fund B — buyable, OtherCo, 100 owners, only sector allocation
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create(FundBIsin),
            Name = "Fund B",
            CompanyName = "OtherCo",
            NumberOfOwners = 100,
            Buyable = true,
            FirstSeenAt = now
        });

        context.FundSectorAllocations.Add(new FundSectorAllocation
        {
            Id = FundSectorAllocationId.New(),
            IsinId = IsinId.Create(FundBIsin),
            SectorId = teknik.Id,
            Percentage = 100.0m
        });

        // Fund C — buyable, TestCo, 200 owners — NO allocations at all (must be excluded)
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create(FundCIsin),
            Name = "Fund C",
            CompanyName = "TestCo",
            NumberOfOwners = 200,
            Buyable = true,
            FirstSeenAt = now
        });

        // Fund D — NON-buyable, TestCo, 300 owners — has allocations but excluded by Buyable filter
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create(FundDIsin),
            Name = "Fund D",
            CompanyName = "TestCo",
            NumberOfOwners = 300,
            Buyable = false,
            FirstSeenAt = now
        });

        context.FundCountryAllocations.Add(new FundCountryAllocation
        {
            Id = FundCountryAllocationId.New(),
            IsinId = IsinId.Create(FundDIsin),
            CountryId = sverige.Id,
            Percentage = 100.0m
        });

        // Fund E — buyable, TestCo, 10 owners — excluded by min-owners filter (when set ≥ 100).
        // Has no allocations either, so even with minOwners=0 it's excluded by the no-allocations rule.
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create(FundEIsin),
            Name = "Fund E",
            CompanyName = "TestCo",
            NumberOfOwners = 10,
            Buyable = true,
            FirstSeenAt = now
        });

        await context.SaveChangesAsync();
        return dbPath;
    }

    /// <summary>
    /// Two-row database: one diacritic country ("Sverige"), one diacritic sector ("Råvaror"),
    /// and one fund holding both. Used to verify ASCII folding in column headers.
    /// </summary>
    private async Task<string> CreateSourceDatabaseWithDiacriticsAsync()
    {
        var dbPath = Path.Combine(_tempDir, "source_diacritics.db");
        await using var context = OpenContext(dbPath);
        await context.Database.EnsureCreatedAsync();

        var sverige = new Country { Id = CountryId.New(), DisplayName = "Sverige", CountryCode = "SE" };
        var ravaror = new Sector { Id = SectorId.New(), DisplayName = "Råvaror" };
        context.Countries.Add(sverige);
        context.Sectors.Add(ravaror);

        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create(FundAIsin),
            Name = "Fund A",
            CompanyName = "TestCo",
            NumberOfOwners = 500,
            Buyable = true,
            FirstSeenAt = DateTimeOffset.UtcNow
        });

        context.FundCountryAllocations.Add(new FundCountryAllocation
        {
            Id = FundCountryAllocationId.New(),
            IsinId = IsinId.Create(FundAIsin),
            CountryId = sverige.Id,
            Percentage = 100.0m
        });

        context.FundSectorAllocations.Add(new FundSectorAllocation
        {
            Id = FundSectorAllocationId.New(),
            IsinId = IsinId.Create(FundAIsin),
            SectorId = ravaror.Id,
            Percentage = 100.0m
        });

        await context.SaveChangesAsync();
        return dbPath;
    }

    /// <summary>
    /// Database with one fund whose name contains a comma — used to verify CSV escaping.
    /// </summary>
    private async Task<string> CreateSourceDatabaseWithCommaNameAsync()
    {
        var dbPath = Path.Combine(_tempDir, "source_comma.db");
        await using var context = OpenContext(dbPath);
        await context.Database.EnsureCreatedAsync();

        var sverige = new Country { Id = CountryId.New(), DisplayName = "Sverige", CountryCode = "SE" };
        context.Countries.Add(sverige);

        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create(FundAIsin),
            Name = "Acme Fund, Class A",
            CompanyName = "TestCo",
            NumberOfOwners = 500,
            Buyable = true,
            FirstSeenAt = DateTimeOffset.UtcNow
        });

        context.FundCountryAllocations.Add(new FundCountryAllocation
        {
            Id = FundCountryAllocationId.New(),
            IsinId = IsinId.Create(FundAIsin),
            CountryId = sverige.Id,
            Percentage = 100.0m
        });

        await context.SaveChangesAsync();
        return dbPath;
    }

    /// <summary>
    /// Two countries that ASCII-fold to the same column suffix — must throw.
    /// </summary>
    private async Task<string> CreateSourceDatabaseWithCollisionAsync()
    {
        var dbPath = Path.Combine(_tempDir, "source_collision.db");
        await using var context = OpenContext(dbPath);
        await context.Database.EnsureCreatedAsync();

        context.Countries.AddRange(
            new Country { Id = CountryId.New(), DisplayName = "Curaçao", CountryCode = "CW" },
            new Country { Id = CountryId.New(), DisplayName = "Curacao", CountryCode = null });

        await context.SaveChangesAsync();
        return dbPath;
    }

    private static YieldRaccoonDbContext OpenContext(string dbPath)
    {
        var connectionString = $"Data Source={dbPath}";
        var optionsBuilder = new DbContextOptionsBuilder<YieldRaccoonDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        return new YieldRaccoonDbContext(optionsBuilder.Options);
    }

    #endregion
}
