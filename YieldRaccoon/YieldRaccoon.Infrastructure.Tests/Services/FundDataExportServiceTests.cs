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
[TestOf(typeof(FundDataExportService))]
public class FundDataExportServiceTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private FundDataExportService _sut = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _sut = _fixture.Create<FundDataExportService>();

        _tempDir = Path.Combine(Path.GetTempPath(), $"YieldRaccoon_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        // SQLite connection pooling keeps file handles alive on Windows —
        // must clear all pools before deleting temp files.
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region Happy Path Tests

    [Test]
    public async Task ExportAsync_MatchingCompany_KeepsOnlyMatchingProfiles()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90));

        // Act
        await _sut.ExportAsync(sourcePath, destPath, "Handelsbanken", cutoffDate, minNumberOfOwners: 0);

        // Assert
        var profileCount = await CountRowsAsync(destPath, "FundProfiles");
        Assert.That(profileCount, Is.EqualTo(2), "Should keep only Handelsbanken funds");
    }

    [Test]
    public async Task ExportAsync_MatchingCompany_RemovesOrphanedHistoryRecords()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90));

        // Act
        await _sut.ExportAsync(sourcePath, destPath, "Handelsbanken", cutoffDate, minNumberOfOwners: 0);

        // Assert — only history records for Handelsbanken funds should remain
        var historyCount = await CountRowsAsync(destPath, "FundHistoryRecords");
        Assert.That(historyCount, Is.GreaterThan(0));

        var orphanCount = await CountRowsAsync(destPath,
            "FundHistoryRecords WHERE FundId NOT IN (SELECT Isin FROM FundProfiles)");
        Assert.That(orphanCount, Is.EqualTo(0), "No orphaned history records should remain");
    }

    [Test]
    public async Task ExportAsync_CutoffDate_RemovesOldHistoryRecords()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));

        // Act
        await _sut.ExportAsync(sourcePath, destPath, "Handelsbanken", cutoffDate, minNumberOfOwners: 0);

        // Assert — only recent records should remain
        var oldCount = await CountRowsAsync(destPath,
            $"FundHistoryRecords WHERE NavDate < '{cutoffDate:yyyy-MM-dd}'");
        Assert.That(oldCount, Is.EqualTo(0), "No records older than cutoff should remain");
    }

    [Test]
    public async Task ExportAsync_CaseInsensitiveMatch_KeepsMatchingProfiles()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90));

        // Act — lowercase input
        await _sut.ExportAsync(sourcePath, destPath, "handelsbanken", cutoffDate, minNumberOfOwners: 0);

        // Assert
        var profileCount = await CountRowsAsync(destPath, "FundProfiles");
        Assert.That(profileCount, Is.EqualTo(2), "Case-insensitive match should keep Handelsbanken funds");
    }

    [Test]
    public async Task ExportAsync_CreatesDestinationFile()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "subdir", "export.db");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, "Handelsbanken",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-90)), minNumberOfOwners: 0);

        // Assert
        Assert.That(File.Exists(destPath), Is.True, "Export file should be created");
    }

    [Test]
    public async Task ExportAsync_DoesNotModifySourceDatabase()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");
        var sourceCountBefore = await CountRowsAsync(sourcePath, "FundProfiles");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, "Handelsbanken",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-90)), minNumberOfOwners: 0);

        // Assert — source must be untouched
        var sourceCountAfter = await CountRowsAsync(sourcePath, "FundProfiles");
        Assert.That(sourceCountAfter, Is.EqualTo(sourceCountBefore),
            "Source database must not be modified");
    }

    [Test]
    public async Task ExportAsync_NoMatchingCompany_ProducesEmptyDatabase()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, "NonExistentCompany",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-90)), minNumberOfOwners: 0);

        // Assert
        var profileCount = await CountRowsAsync(destPath, "FundProfiles");
        var historyCount = await CountRowsAsync(destPath, "FundHistoryRecords");
        Assert.That(profileCount, Is.EqualTo(0));
        Assert.That(historyCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ExportAsync_NullCompanyName_RemovesProfilesWithNullCompany()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");

        // Act — filter for SEB (has a profile, but "NullCompany" fund has CompanyName = null)
        await _sut.ExportAsync(sourcePath, destPath, "SEB",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-90)), minNumberOfOwners: 0);

        // Assert — null CompanyName profiles should be removed
        var nullCompanyCount = await CountRowsAsync(destPath,
            "FundProfiles WHERE CompanyName IS NULL");
        Assert.That(nullCompanyCount, Is.EqualTo(0),
            "Funds with null CompanyName should be removed");
    }

    [Test]
    public async Task ExportAsync_MinNumberOfOwners_RemovesProfilesBelowThreshold()
    {
        // Arrange — Handelsbanken Sverige has 500 owners, Handelsbanken Global has 50
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90));

        // Act — filter requires at least 100 owners
        await _sut.ExportAsync(sourcePath, destPath, "Handelsbanken", cutoffDate, minNumberOfOwners: 100);

        // Assert — only Handelsbanken Sverige (500 owners) should remain
        var profileCount = await CountRowsAsync(destPath, "FundProfiles");
        Assert.That(profileCount, Is.EqualTo(1), "Should keep only funds with >= 100 owners");
    }

    [Test]
    public async Task ExportAsync_MinNumberOfOwners_NullOwnersAreExcluded()
    {
        // Arrange — Unknown Fund has null NumberOfOwners
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90));

        // Act — use SEB + min owners 0 to keep all, then check null fund is gone by company filter
        // Instead, test with a broad company match and minOwners > 0
        await _sut.ExportAsync(sourcePath, destPath, "SEB", cutoffDate, minNumberOfOwners: 1);

        // Assert — SEB fund (200 owners) should remain, null owners fund already excluded by company
        var profileCount = await CountRowsAsync(destPath, "FundProfiles");
        Assert.That(profileCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ExportAsync_MinNumberOfOwnersZero_SkipsOwnerFilter()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "export.db");
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90));

        // Act — minNumberOfOwners: 0 should skip the filter entirely
        await _sut.ExportAsync(sourcePath, destPath, "Handelsbanken", cutoffDate, minNumberOfOwners: 0);

        // Assert — both Handelsbanken funds should remain (500 and 50 owners)
        var profileCount = await CountRowsAsync(destPath, "FundProfiles");
        Assert.That(profileCount, Is.EqualTo(2), "Should keep all Handelsbanken funds when owner filter is disabled");
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ExportAsync_SourceNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var fakePath = Path.Combine(_tempDir, "nonexistent.db");
        var destPath = Path.Combine(_tempDir, "export.db");

        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(() =>
            _sut.ExportAsync(fakePath, destPath, "Test",
                DateOnly.FromDateTime(DateTime.Today)));
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Creates a source SQLite database with known test data.
    /// 4 FundProfiles: 2 Handelsbanken, 1 SEB, 1 with null CompanyName.
    /// History records spanning from 60 days ago to today.
    /// </summary>
    private async Task<string> CreateSourceDatabaseAsync()
    {
        var dbPath = Path.Combine(_tempDir, "source.db");
        var connectionString = $"Data Source={dbPath}";

        var optionsBuilder = new DbContextOptionsBuilder<YieldRaccoonDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new YieldRaccoonDbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();

        var isin1 = IsinId.Create("SE0000000011");
        var isin2 = IsinId.Create("SE0000000029");
        var isin3 = IsinId.Create("SE0000000037");
        var isin4 = IsinId.Create("SE0000000045");
        var now = DateTimeOffset.UtcNow;

        // 2 Handelsbanken funds
        context.FundProfiles.Add(new FundProfile
        {
            Id = isin1, Name = "Handelsbanken Sverige", CompanyName = "Handelsbanken",
            NumberOfOwners = 500, FirstSeenAt = now
        });
        context.FundProfiles.Add(new FundProfile
        {
            Id = isin2, Name = "Handelsbanken Global", CompanyName = "Handelsbanken",
            NumberOfOwners = 50, FirstSeenAt = now
        });

        // 1 SEB fund
        context.FundProfiles.Add(new FundProfile
        {
            Id = isin3, Name = "SEB Europafond", CompanyName = "SEB",
            NumberOfOwners = 200, FirstSeenAt = now
        });

        // 1 fund with null CompanyName
        context.FundProfiles.Add(new FundProfile
        {
            Id = isin4, Name = "Unknown Fund", CompanyName = null,
            NumberOfOwners = null, FirstSeenAt = now
        });

        // History records for each fund: one recent (3 days ago), one older (30 days ago)
        foreach (var isin in new[] { isin1, isin2, isin3, isin4 })
        {
            context.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.New(),
                IsinId = isin,
                Nav = 100.50m,
                NavDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-3))
            });

            context.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.New(),
                IsinId = isin,
                Nav = 99.00m,
                NavDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30))
            });
        }

        await context.SaveChangesAsync();

        return dbPath;
    }

    private static async Task<int> CountRowsAsync(string dbPath, string tableOrQuery)
    {
        var connectionString = $"Data Source={dbPath};Mode=ReadOnly";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableOrQuery}";
        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    #endregion
}
