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

        _userSettingsServiceMock = new Mock<IUserSettingsService>();
    }

    private FundStatisticsExportWindowViewModel CreateSut(AutoStartOptions autoStartOptions)
    {
        return new FundStatisticsExportWindowViewModel(
            _logger,
            _exportServiceMock.Object,
            _metadataServiceMock.Object,
            _databaseOptions,
            _userSettingsServiceMock.Object,
            _userSettings,
            autoStartOptions);
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
