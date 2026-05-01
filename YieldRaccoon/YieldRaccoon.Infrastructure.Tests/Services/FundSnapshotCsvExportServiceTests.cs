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
[TestOf(typeof(FundSnapshotCsvExportService))]
public class FundSnapshotCsvExportServiceTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private FundSnapshotCsvExportService _sut = null!;
    private string _tempDir = null!;

    private const string ExpectedHeader =
        "isin,as_of_date," +
        "return_12w_compound_pct,ann_volatility_12w_pct,sharpe_12w,max_drawdown_12w_pct," +
        "return_1y_compound_pct,ann_volatility_1y_pct,sharpe_1y,max_drawdown_1y_pct";

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _sut = _fixture.Create<FundSnapshotCsvExportService>();

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
    public async Task ExportAsync_WritesExpectedHeader()
    {
        var sourcePath = await CreateSourceDatabaseAsync(navHistoryDays: 90);
        var destPath = Path.Combine(_tempDir, "snapshot.csv");

        await _sut.ExportAsync(sourcePath, destPath);

        var firstLine = (await File.ReadAllLinesAsync(destPath))[0];
        Assert.That(firstLine, Is.EqualTo(ExpectedHeader));
    }

    [Test]
    public async Task ExportAsync_AnchorsAsOfDateOnEveryRow()
    {
        var sourcePath = await CreateSourceDatabaseAsync(navHistoryDays: 90);
        var destPath = Path.Combine(_tempDir, "snapshot.csv");

        await _sut.ExportAsync(sourcePath, destPath);

        var rows = (await File.ReadAllLinesAsync(destPath)).Skip(1).ToArray();
        Assert.That(rows, Is.Not.Empty);

        var asOfDates = rows.Select(r => r.Split(',')[1]).Distinct().ToArray();
        Assert.That(asOfDates.Length, Is.EqualTo(1), "as_of_date must be identical on every row");
    }

    [Test]
    public async Task ExportAsync_FundWithShortHistory_Returns_NaN_For_OneYear_Columns()
    {
        // Fund only has 90 days of NAV — too short for 1y horizon (365d).
        var sourcePath = await CreateSourceDatabaseAsync(navHistoryDays: 90);
        var destPath = Path.Combine(_tempDir, "snapshot.csv");

        await _sut.ExportAsync(sourcePath, destPath);

        var rows = (await File.ReadAllLinesAsync(destPath)).Skip(1).ToArray();
        var firstRow = rows[0].Split(',');
        // Columns 6..9 are the 1y_* metrics
        for (var i = 6; i <= 9; i++)
            Assert.That(firstRow[i], Is.EqualTo("NaN"), $"Column index {i} (1y metric) must be NaN for short-history fund");
    }

    [Test]
    public async Task ExportAsync_EmptyDatabase_WritesHeaderOnly()
    {
        var sourcePath = await CreateEmptySourceDatabaseAsync();
        var destPath = Path.Combine(_tempDir, "snapshot.csv");

        var rowCount = await _sut.ExportAsync(sourcePath, destPath);

        Assert.That(rowCount, Is.EqualTo(0));
        var lines = await File.ReadAllLinesAsync(destPath);
        Assert.That(lines.Length, Is.EqualTo(1));
        Assert.That(lines[0], Is.EqualTo(ExpectedHeader));
    }

    [Test]
    public async Task ExportAsync_BuyableFilter_ExcludesNonBuyableFunds()
    {
        var sourcePath = await CreateSourceDatabaseAsync(navHistoryDays: 90);
        var destPath = Path.Combine(_tempDir, "snapshot.csv");

        await _sut.ExportAsync(sourcePath, destPath);

        var content = await File.ReadAllTextAsync(destPath);
        Assert.That(content, Does.Not.Contain("SE0000000029"), "Non-buyable fund must be excluded");
    }

    private async Task<string> CreateSourceDatabaseAsync(int navHistoryDays)
    {
        var dbPath = Path.Combine(_tempDir, "source.db");
        var connectionString = $"Data Source={dbPath}";

        var optionsBuilder = new DbContextOptionsBuilder<YieldRaccoonDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new YieldRaccoonDbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();

        var now = DateTimeOffset.UtcNow;
        var buyableIsin = IsinId.Create("SE0000000011");
        var nonBuyableIsin = IsinId.Create("SE0000000029");

        context.FundProfiles.Add(new FundProfile
        {
            Id = buyableIsin, Name = "Buyable Fund", CompanyName = "TestCo",
            NumberOfOwners = 500, Buyable = true, FirstSeenAt = now
        });
        context.FundProfiles.Add(new FundProfile
        {
            Id = nonBuyableIsin, Name = "Non-Buyable Fund", CompanyName = "TestCo",
            NumberOfOwners = 300, Buyable = false, FirstSeenAt = now
        });

        var anchorDate = DateOnly.FromDateTime(DateTime.Today);
        foreach (var isin in new[] { buyableIsin, nonBuyableIsin })
        {
            for (var i = 0; i < navHistoryDays; i++)
            {
                context.FundHistoryRecords.Add(new FundHistoryRecord
                {
                    Id = FundHistoryRecordId.New(),
                    IsinId = isin,
                    Nav = 100m + i * 0.1m,
                    NavDate = anchorDate.AddDays(-i)
                });
            }
        }

        await context.SaveChangesAsync();
        return dbPath;
    }

    private async Task<string> CreateEmptySourceDatabaseAsync()
    {
        var dbPath = Path.Combine(_tempDir, "empty.db");
        var connectionString = $"Data Source={dbPath}";

        var optionsBuilder = new DbContextOptionsBuilder<YieldRaccoonDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new YieldRaccoonDbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();
        await context.SaveChangesAsync();
        return dbPath;
    }
}
