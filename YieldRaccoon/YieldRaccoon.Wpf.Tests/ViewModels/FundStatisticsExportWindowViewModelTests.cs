using Moq;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Models;
using YieldRaccoon.Wpf.Services;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf.Tests.ViewModels;

[TestFixture]
[TestOf(typeof(FundStatisticsExportWindowViewModel))]
public class FundStatisticsExportWindowViewModelTests
{
    
    private Mock<IFundStatisticsCsvExportService> _exportServiceMock;
    private Mock<IFundMetadataCsvExportService> _metadataServiceMock;
    private Mock<IFundSnapshotCsvExportService> _snapshotServiceMock;
    private Mock<IUserSettingsService> _userSettingsServiceMock;
    private ILogger _logger;
    private DatabaseOptions _databaseOptions;
    private UserSettings _userSettings;

    [SetUp]
    public void SetUp()
    {
        _logger = LogManager.GetCurrentClassLogger();

        _databaseOptions = new DatabaseOptions
        {
            Provider = DatabaseProvider.SQLite,
            ConnectionString = $"Data Source={Path.Combine(Path.GetTempPath(), "YieldRaccoon.Tests.db")}"
        };

        _userSettings = new UserSettings();

        _exportServiceMock = new Mock<IFundStatisticsCsvExportService>();
        _exportServiceMock
            .Setup(x => x.ExportAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<IProgress<(int processed, int total)>?>()))
            .ReturnsAsync(0);

        _metadataServiceMock = new Mock<IFundMetadataCsvExportService>();
        _metadataServiceMock
            .Setup(x => x.ExportAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int>()))
            .ReturnsAsync(0);

        _snapshotServiceMock = new Mock<IFundSnapshotCsvExportService>();
        _snapshotServiceMock
            .Setup(x => x.ExportAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<IProgress<(int processed, int total)>?>()))
            .ReturnsAsync(0);

        _userSettingsServiceMock = new Mock<IUserSettingsService>();
    }

    private FundStatisticsExportWindowViewModel CreateSut(AutoStartOptions autoStartOptions)
    {
        return new FundStatisticsExportWindowViewModel(
            _logger,
            _exportServiceMock.Object,
            _metadataServiceMock.Object,
            _snapshotServiceMock.Object,
            _databaseOptions,
            _userSettingsServiceMock.Object,
            _userSettings,
            autoStartOptions);
    }

    [Test]
    public void DefaultPaths_AreBuiltWithIsoWeekAndAllFamilyTags()
    {
        // Arrange
        var sut = CreateSut(new AutoStartOptions());
        var expectedIsoWeek = $"{System.Globalization.ISOWeek.GetYear(DateTime.Now):D4}-W{System.Globalization.ISOWeek.GetWeekOfYear(DateTime.Now):D2}";

        // Assert — default filenames carry the ISO week tag and the "all" family
        Assert.That(Path.GetFileName(sut.OutputPath), Is.EqualTo($"YieldRaccoon_summary_all_{expectedIsoWeek}.csv"));
        Assert.That(Path.GetFileName(sut.SnapshotOutputPath), Is.EqualTo($"YieldRaccoon_snapshot_all_{expectedIsoWeek}.csv"));
        Assert.That(Path.GetFileName(sut.MetadataOutputPath), Is.EqualTo($"YieldRaccoon_metadata_all_{expectedIsoWeek}.csv"));
    }

    [Test]
    public void DefaultPaths_UseLowerCasedCompanyFamilyTagWhenFilterSet()
    {
        // Arrange
        var sut = CreateSut(new AutoStartOptions());

        // Act — set a company filter; default paths refresh
        sut.CompanyName = "Schroder";

        // Assert
        Assert.That(Path.GetFileName(sut.OutputPath), Does.Contain("_schroder_"));
        Assert.That(Path.GetFileName(sut.SnapshotOutputPath), Does.Contain("_schroder_"));
        Assert.That(Path.GetFileName(sut.MetadataOutputPath), Does.Contain("_schroder_"));
        Assert.That(Path.GetFileName(sut.OutputPath), Does.Not.Contain("_yyyy-"),
            "Filename must not carry a yyyy-MM-dd date suffix — ISO week is the only version tag");
    }

    [Test]
    public void OnOpen_StaleV1PersistedPaths_AreIgnored_AndRebuiltAsV2Defaults()
    {
        // Regression: persisted paths from v1 (e.g., "..._2weeks_1year.csv") must NOT be loaded
        // on open — they are stale and use the legacy filename grammar. The window should always
        // present current-ISO-week defaults instead.
        _userSettings.StatsExportOutputPath = "C:/old/YieldRaccoon_summary_2weeks_1year.csv";
        _userSettings.StatsExportSnapshotOutputPath = "C:/old/YieldRaccoon_snapshot_2weeks_1year.csv";
        _userSettings.StatsExportMetadataOutputPath = "C:/old/YieldRaccoon_metadata.csv";

        var sut = CreateSut(new AutoStartOptions());

        Assert.That(sut.OutputPath, Does.Not.Contain("2weeks_1year"));
        Assert.That(sut.SnapshotOutputPath, Does.Not.Contain("2weeks_1year"));
        Assert.That(sut.MetadataOutputPath, Does.Not.Contain("2weeks_1year"));
        Assert.That(Path.GetFileName(sut.OutputPath), Does.StartWith("YieldRaccoon_summary_all_"));
        Assert.That(Path.GetFileName(sut.SnapshotOutputPath), Does.StartWith("YieldRaccoon_snapshot_all_"));
        Assert.That(Path.GetFileName(sut.MetadataOutputPath), Does.StartWith("YieldRaccoon_metadata_all_"));
    }

    [Test]
    public void EditingCompanyName_RefreshesAllThreeDefaultPaths()
    {
        // Regression: typing a company name into the filter must update all three filenames
        // immediately (UpdateSourceTrigger=PropertyChanged on the TextBox binding).
        var sut = CreateSut(new AutoStartOptions());
        Assert.That(Path.GetFileName(sut.OutputPath), Does.Contain("_all_"));

        sut.CompanyName = "Schroder";

        Assert.That(Path.GetFileName(sut.OutputPath), Does.Contain("_schroder_"));
        Assert.That(Path.GetFileName(sut.SnapshotOutputPath), Does.Contain("_schroder_"));
        Assert.That(Path.GetFileName(sut.MetadataOutputPath), Does.Contain("_schroder_"));
        Assert.That(Path.GetFileName(sut.OutputPath), Does.Not.Contain("_all_"));
    }

    [Test]
    public void DefaultPaths_DoNotContainYyyyMmDdDateSuffix()
    {
        // Regression guard: the auto-weekly run used to append `_yyyy-MM-dd` to the filename.
        // ISO week is now the version tag — no calendar-date suffix anywhere.
        var sut = CreateSut(new AutoStartOptions { AutoWeeklyStats = true });

        var todayIsoSegment = DateTime.Now.ToString("yyyy-MM-dd");
        Assert.That(sut.OutputPath, Does.Not.Contain($"_{todayIsoSegment}"));
        Assert.That(sut.SnapshotOutputPath, Does.Not.Contain($"_{todayIsoSegment}"));
        Assert.That(sut.MetadataOutputPath, Does.Not.Contain($"_{todayIsoSegment}"));
    }

    [Test]
    public void ExecuteLoaded_WhenAutoWeeklyStatsIsTrue_ClearsFlagSoSubsequentManualOpensDoNotAutoFire()
    {
        // Arrange — mirror the production setup: AutoStartOptions is a DI singleton
        // whose AutoWeeklyStats flag is flipped on when a scheduled weekly run is about
        // to happen. After the VM consumes the flag, it must be cleared — otherwise a
        // later manual open from the menu spins up a fresh VM, reads the still-true
        // flag, auto-fires Export, and auto-closes, so the window appears to vanish.
        var autoStartOptions = new AutoStartOptions { AutoWeeklyStats = true };
        var sut = CreateSut(autoStartOptions);

        // Act
        sut.LoadedCommand.Execute(null);

        // Assert
        Assert.That(autoStartOptions.AutoWeeklyStats, Is.False,
            "AutoWeeklyStats must be consumed exactly once; leaving it true causes the next manual open to auto-fire Export and auto-close.");
    }
}
