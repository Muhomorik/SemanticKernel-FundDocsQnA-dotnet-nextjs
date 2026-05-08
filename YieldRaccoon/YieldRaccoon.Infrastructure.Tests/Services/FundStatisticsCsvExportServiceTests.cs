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
[TestOf(typeof(FundStatisticsCsvExportService))]
public class FundStatisticsCsvExportServiceTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private FundStatisticsCsvExportService _sut = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _sut = _fixture.Create<FundStatisticsCsvExportService>();

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

    [Test]
    public async Task ExportAsync_BuyableFilter_ExcludesNonBuyableFunds()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "stats.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, windowSizeDays: 30, minNumberOfOwners: 0);

        // Assert — non-buyable fund ISIN should not appear
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Not.Contain("SE0000000029"), "Non-buyable fund should be excluded");
        Assert.That(content, Does.Not.Contain("SE0000000045"), "Null-buyable fund should be excluded");
    }

    [Test]
    public async Task ExportAsync_BuyableFilter_IncludesBuyableFunds()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "stats.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, windowSizeDays: 30, minNumberOfOwners: 0);

        // Assert — buyable fund with enough NAV data should appear
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Contain("SE0000000011"), "Buyable fund should be included");
    }

    [Test]
    public async Task ExportAsync_WritesV2HeaderWithRenamedColumns()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "stats.csv");

        // Act
        await _sut.ExportAsync(sourcePath, destPath, windowSizeDays: 30, minNumberOfOwners: 0);

        // Assert — first line is the v2 header
        var firstLine = (await File.ReadAllLinesAsync(destPath))[0];
        Assert.That(firstLine, Is.EqualTo(
            "isin,name,period_start,period_end,first_nav,last_nav,nav_high,nav_low," +
            "return_2w_pct,ann_volatility_2w_pct,max_drawdown_2w_pct,current_drawdown_pct,sharpe_2w," +
            "best_day_pct,worst_day_pct,pct_positive_days,skewness"));
    }

    [Test]
    public void SliceIntoWindows_DropsTrailingPartialBucketBelow7Days()
    {
        // Arrange — 14-day windows. Series spans ~17 days; the last 3 days form a partial bucket.
        var series = new List<(DateOnly date, decimal nav)>
        {
            (new DateOnly(2026, 1, 1), 100m),
            (new DateOnly(2026, 1, 5), 101m),
            (new DateOnly(2026, 1, 10), 102m),
            (new DateOnly(2026, 1, 14), 103m),
            // New window starts here (>=14 days from 2026-01-01)
            (new DateOnly(2026, 1, 15), 104m),
            (new DateOnly(2026, 1, 17), 105m),  // Only 3-day span — should be dropped
        };

        // Act
        var windows = FundStatisticsCsvExportService.SliceIntoWindows(series, windowSizeDays: 14);

        // Assert — the partial trailing window must not appear
        Assert.That(windows.Count, Is.EqualTo(1), "Partial trailing bucket (<7 days) must be dropped");
        Assert.That(windows[0][0].date, Is.EqualTo(new DateOnly(2026, 1, 1)));
    }

    [Test]
    public void SliceIntoWindows_KeepsTrailingBucketOf7DaysOrMore()
    {
        // Arrange — 14-day windows; trailing window spans exactly 7 days → keep
        var series = new List<(DateOnly date, decimal nav)>
        {
            (new DateOnly(2026, 1, 1), 100m),
            (new DateOnly(2026, 1, 14), 102m),
            (new DateOnly(2026, 1, 15), 103m),
            (new DateOnly(2026, 1, 22), 105m),  // 7-day trailing window
        };

        // Act
        var windows = FundStatisticsCsvExportService.SliceIntoWindows(series, windowSizeDays: 14);

        // Assert
        Assert.That(windows.Count, Is.EqualTo(2), "Trailing window of 7+ days must be emitted");
    }

    [Test]
    public async Task ExportAsync_AllFilters_BuyableAndCompanyAndOwners()
    {
        // Arrange
        var sourcePath = await CreateSourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "stats.csv");

        // Act — filter by TestCo + min 100 owners
        await _sut.ExportAsync(sourcePath, destPath, windowSizeDays: 30,
            companyName: "TestCo", minNumberOfOwners: 100);

        // Assert — only SE0000000011 (TestCo, buyable, 500 owners)
        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Contain("SE0000000011"));
        Assert.That(content, Does.Not.Contain("SE0000000029"), "Non-buyable TestCo fund excluded");
        Assert.That(content, Does.Not.Contain("SE0000000037"), "OtherCo fund excluded by company filter");
    }

    #region Test Helpers

    /// <summary>
    /// Creates a source SQLite database with test data for Buyable filter testing.
    /// Includes funds with varying Buyable values and enough NAV history for windowed stats.
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
        var isin1 = IsinId.Create("SE0000000011");
        var isin2 = IsinId.Create("SE0000000029");
        var isin3 = IsinId.Create("SE0000000037");
        var isin4 = IsinId.Create("SE0000000045");

        // Buyable fund
        context.FundProfiles.Add(new FundProfile
        {
            Id = isin1, Name = "Buyable Fund", CompanyName = "TestCo",
            NumberOfOwners = 500, Buyable = true, FirstSeenAt = now
        });

        // Non-buyable fund
        context.FundProfiles.Add(new FundProfile
        {
            Id = isin2, Name = "Non-Buyable Fund", CompanyName = "TestCo",
            NumberOfOwners = 300, Buyable = false, FirstSeenAt = now
        });

        // Buyable fund from different company
        context.FundProfiles.Add(new FundProfile
        {
            Id = isin3, Name = "Other Buyable Fund", CompanyName = "OtherCo",
            NumberOfOwners = 50, Buyable = true, FirstSeenAt = now
        });

        // Null-buyable fund
        context.FundProfiles.Add(new FundProfile
        {
            Id = isin4, Name = "Null Buyable Fund", CompanyName = "TestCo",
            NumberOfOwners = 200, Buyable = null, FirstSeenAt = now
        });

        // NAV history for all funds — need at least 2 data points within a 30-day window
        foreach (var isin in new[] { isin1, isin2, isin3, isin4 })
        {
            for (var i = 0; i < 5; i++)
            {
                context.FundHistoryRecords.Add(new FundHistoryRecord
                {
                    Id = FundHistoryRecordId.New(),
                    IsinId = isin,
                    Nav = 100m + i * 0.5m,
                    NavDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-i * 5))
                });
            }
        }

        await context.SaveChangesAsync();
        return dbPath;
    }

    #endregion
}
