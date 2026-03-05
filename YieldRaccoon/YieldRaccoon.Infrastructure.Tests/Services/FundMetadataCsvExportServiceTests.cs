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
[TestOf(typeof(FundMetadataCsvExportService))]
public class FundMetadataCsvExportServiceTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private FundMetadataCsvExportService _sut = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _sut = _fixture.Create<FundMetadataCsvExportService>();

        _tempDir = Path.Combine(Path.GetTempPath(), $"YieldRaccoon_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region Happy Path Tests

    [Test]
    public async Task ExportAsync_WithBuyableFunds_WritesCsvWithCorrectHeaders()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert
        var lines = await File.ReadAllLinesAsync(destPath);
        Assert.That(lines[0], Is.EqualTo(
            "isin,name,company_name,currency_code,category,fund_type,is_index_fund,managed_type," +
            "total_fee,management_fee,risk,rating,sharpe_ratio,standard_deviation," +
            "recommended_holding_period,capital,number_of_owners"));
    }

    [Test]
    public async Task ExportAsync_WithBuyableFunds_ReturnsCorrectRowCount()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        var rowCount = await _sut.ExportAsync(sourcePath, destPath);

        // Assert — only 2 buyable funds with owners >= 0
        Assert.That(rowCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ExportAsync_BuyableFilter_ExcludesNonBuyableFunds()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert — non-buyable fund ISIN should not appear
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Not.Contain("SE0000000029"), "Non-buyable fund should be excluded");
    }

    [Test]
    public async Task ExportAsync_BuyableFilter_ExcludesNullBuyableFunds()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert — null-buyable fund ISIN should not appear
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Not.Contain("SE0000000045"), "Null-buyable fund should be excluded");
    }

    [Test]
    public async Task ExportAsync_BuyableFilter_IncludesBuyableFunds()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert — buyable funds should appear
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Contain("SE0000000011"), "Buyable fund 1 should be included");
        Assert.That(content, Does.Contain("SE0000000037"), "Buyable fund 2 should be included");
    }

    [Test]
    public async Task ExportAsync_CompanyFilter_KeepsOnlyMatchingCompany()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, companyName: "TestCo");

        // Assert — only TestCo buyable fund (SE0000000011)
        var rowCount = await CountCsvDataRows(destPath);
        Assert.That(rowCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ExportAsync_NullCompanyName_IncludesAllBuyableFunds()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, companyName: null);

        // Assert — both buyable funds
        var rowCount = await CountCsvDataRows(destPath);
        Assert.That(rowCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ExportAsync_MinOwners_ExcludesFundsBelowThreshold()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act — require >= 400 owners
        await _sut.ExportAsync(sourcePath, destPath, minNumberOfOwners: 400);

        // Assert — only fund with 500 owners
        var rowCount = await CountCsvDataRows(destPath);
        Assert.That(rowCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ExportAsync_MinOwnersZero_SkipsOwnerFilter()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, minNumberOfOwners: 0);

        // Assert — both buyable funds (even the one with 50 owners)
        var rowCount = await CountCsvDataRows(destPath);
        Assert.That(rowCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ExportAsync_NullableFields_WritesEmptyStringsForNulls()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, companyName: "OtherCo");

        // Assert — SE0000000037 has many null fields
        var lines = await File.ReadAllLinesAsync(destPath);
        Assert.That(lines.Length, Is.EqualTo(2)); // header + 1 data row
        var dataLine = lines[1];
        // Category, FundType, ManagedType etc. are null → empty fields
        Assert.That(dataLine, Does.Contain("SE0000000037"));
        // Count commas — should have exactly 16 commas (17 fields)
        var commaCount = dataLine.Count(c => c == ',');
        Assert.That(commaCount, Is.EqualTo(16));
    }

    [Test]
    public async Task ExportAsync_CsvFieldsWithCommas_AreProperlyEscaped()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseWithCommaNameAsync();
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert — the name containing a comma should be quoted
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Contain("\"Fund A, Inc.\""));
    }

    [Test]
    public async Task ExportAsync_CreatesOutputDirectory()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "subdir", "nested", "metadata.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath);

        // Assert
        Assert.That(File.Exists(destPath), Is.True);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ExportAsync_SourceNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var fakePath = Path.Combine(_tempDir, "nonexistent.db");
        var destPath = Path.Combine(_tempDir, "metadata.csv");

        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(() =>
            _sut.ExportAsync(fakePath, destPath));
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Creates a source SQLite database with known test data.
    /// 4 FundProfiles: 1 buyable (TestCo, 500 owners), 1 non-buyable, 1 buyable (OtherCo, 50 owners), 1 null-buyable.
    /// </summary>
    private async Task<string> CreateSourceDatabaseAsync()
    {
        var dbPath = Path.Combine(_tempDir, "source.db");
        var connectionString = $"Data Source={dbPath}";

        var optionsBuilder = new DbContextOptionsBuilder<YieldRaccoonDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new YieldRaccoonDbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();

        var now = DateTimeOffset.UtcNow;

        // Buyable fund with full metadata
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create("SE0000000011"),
            Name = "Test Equity Fund",
            CompanyName = "TestCo",
            CurrencyCode = "SEK",
            Category = "Equity",
            FundType = "Equity Fund",
            IsIndexFund = true,
            ManagedType = "PASSIVE",
            TotalFee = 0.0125m,
            ManagementFee = 0.01m,
            Risk = 4,
            Rating = 3,
            SharpeRatio = 1.25m,
            StandardDeviation = 12.5m,
            RecommendedHoldingPeriod = "5 years",
            Capital = 1_000_000m,
            NumberOfOwners = 500,
            Buyable = true,
            FirstSeenAt = now
        });

        // Non-buyable fund
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create("SE0000000029"),
            Name = "Non-Buyable Fund",
            CompanyName = "TestCo",
            NumberOfOwners = 300,
            Buyable = false,
            FirstSeenAt = now
        });

        // Buyable fund with sparse metadata (many nulls)
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create("SE0000000037"),
            Name = "Sparse Fund",
            CompanyName = "OtherCo",
            NumberOfOwners = 50,
            Buyable = true,
            FirstSeenAt = now
        });

        // Null-buyable fund
        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create("SE0000000045"),
            Name = "Unknown Fund",
            CompanyName = "OtherCo",
            NumberOfOwners = 200,
            Buyable = null,
            FirstSeenAt = now
        });

        await context.SaveChangesAsync();
        return dbPath;
    }

    /// <summary>
    /// Creates a source database with a fund whose name contains a comma (for CSV escaping tests).
    /// </summary>
    private async Task<string> CreateSourceDatabaseWithCommaNameAsync()
    {
        var dbPath = Path.Combine(_tempDir, "source_comma.db");
        var connectionString = $"Data Source={dbPath}";

        var optionsBuilder = new DbContextOptionsBuilder<YieldRaccoonDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new YieldRaccoonDbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();

        context.FundProfiles.Add(new FundProfile
        {
            Id = IsinId.Create("SE0000000011"),
            Name = "Fund A, Inc.",
            CompanyName = "TestCo",
            NumberOfOwners = 100,
            Buyable = true,
            FirstSeenAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();
        return dbPath;
    }

    private static async Task<int> CountCsvDataRows(string csvPath)
    {
        var lines = await File.ReadAllLinesAsync(csvPath);
        return lines.Length - 1; // Subtract header row
    }

    #endregion
}
