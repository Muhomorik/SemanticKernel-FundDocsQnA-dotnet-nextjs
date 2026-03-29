using System.Text.Json;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Services;

namespace YieldRaccoon.Wpf.Tests.Services;

[TestFixture]
[TestOf(typeof(UserSettingsService))]
public class UserSettingsServiceTests
{
    private ILogger _logger;
    private string _tempDirectory;
    private string _settingsFilePath;
    private UserSettingsService _sut;

    [SetUp]
    public void SetUp()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "YieldRaccoon.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _settingsFilePath = Path.Combine(_tempDirectory, "settings.json");
        _sut = new UserSettingsService(_logger, _settingsFilePath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    #region Constructor

    [Test]
    public void Constructor_WithCustomPath_SetsSettingsFilePath()
    {
        // Assert
        Assert.That(_sut.SettingsFilePath, Is.EqualTo(_settingsFilePath));
    }

    [Test]
    public void Constructor_WithoutCustomPath_UsesDefaultLocalAppDataPath()
    {
        // Arrange & Act
        var sut = new UserSettingsService(_logger);

        // Assert
        Assert.That(sut.SettingsFilePath, Does.Contain("YieldRaccoon"));
        Assert.That(sut.SettingsFilePath, Does.EndWith("settings.json"));
    }

    #endregion

    #region Load

    [Test]
    public void Load_FileDoesNotExist_ReturnsDefaultSettings()
    {
        // Act
        var result = _sut.Load();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DatabasePath, Is.Null);
        Assert.That(result.DatabaseProvider, Is.Null);
    }

    [Test]
    public void Load_ValidJsonWithDatabasePath_ReturnsSettingsWithPath()
    {
        // Arrange
        var json = """{ "databasePath": "C:\\custom\\path.db" }""";
        File.WriteAllText(_settingsFilePath, json);

        // Act
        var result = _sut.Load();

        // Assert
        Assert.That(result.DatabasePath, Is.EqualTo(@"C:\custom\path.db"));
    }

    [Test]
    public void Load_ValidJsonWithDatabaseProvider_ReturnsSettingsWithProvider()
    {
        // Arrange
        var json = """{ "databaseProvider": "SQLite" }""";
        File.WriteAllText(_settingsFilePath, json);

        // Act
        var result = _sut.Load();

        // Assert
        Assert.That(result.DatabaseProvider, Is.EqualTo(DatabaseProvider.SQLite));
    }

    [Test]
    public void Load_ValidJsonWithAllProperties_ReturnsCompleteSettings()
    {
        // Arrange
        var json = """{ "databasePath": "my.db", "databaseProvider": "DualWrite" }""";
        File.WriteAllText(_settingsFilePath, json);

        // Act
        var result = _sut.Load();

        // Assert
        Assert.That(result.DatabasePath, Is.EqualTo("my.db"));
        Assert.That(result.DatabaseProvider, Is.EqualTo(DatabaseProvider.DualWrite));
    }

    [Test]
    public void Load_InvalidJson_ReturnsDefaultSettings()
    {
        // Arrange
        File.WriteAllText(_settingsFilePath, "not valid json {{{");

        // Act
        var result = _sut.Load();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DatabasePath, Is.Null);
        Assert.That(result.DatabaseProvider, Is.Null);
    }

    [Test]
    public void Load_EmptyJson_ReturnsDefaultSettings()
    {
        // Arrange
        File.WriteAllText(_settingsFilePath, "{}");

        // Act
        var result = _sut.Load();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DatabasePath, Is.Null);
        Assert.That(result.DatabaseProvider, Is.Null);
    }

    #endregion

    #region Save

    [Test]
    public void Save_WithDatabasePath_WritesJsonFile()
    {
        // Arrange
        var settings = new UserSettings { DatabasePath = @"C:\data\test.db" };

        // Act
        _sut.Save(settings);

        // Assert
        Assert.That(File.Exists(_settingsFilePath), Is.True);
        var json = File.ReadAllText(_settingsFilePath);
        Assert.That(json, Does.Contain("databasePath"));
        Assert.That(json, Does.Contain(@"C:\\data\\test.db"));
    }

    [Test]
    public void Save_WithDatabaseProvider_WritesEnumAsString()
    {
        // Arrange
        var settings = new UserSettings { DatabaseProvider = DatabaseProvider.SQLite };

        // Act
        _sut.Save(settings);

        // Assert
        var json = File.ReadAllText(_settingsFilePath);
        Assert.That(json, Does.Contain("\"SQLite\""));
        Assert.That(json, Does.Not.Contain("\"1\"")); // Not serialized as integer
    }

    [Test]
    public void Save_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nestedDir = Path.Combine(_tempDirectory, "nested", "dir");
        var nestedPath = Path.Combine(nestedDir, "settings.json");
        var sut = new UserSettingsService(_logger, nestedPath);
        var settings = new UserSettings { DatabasePath = "test.db" };

        // Act
        sut.Save(settings);

        // Assert
        Assert.That(File.Exists(nestedPath), Is.True);
    }

    #endregion

    #region Round-Trip

    [Test]
    public void SaveThenLoad_AllProperties_RoundTripsCorrectly()
    {
        // Arrange
        var original = new UserSettings
        {
            DatabasePath = @"C:\my\database.db",
            DatabaseProvider = DatabaseProvider.DualWrite
        };

        // Act
        _sut.Save(original);
        var loaded = _sut.Load();

        // Assert
        Assert.That(loaded.DatabasePath, Is.EqualTo(original.DatabasePath));
        Assert.That(loaded.DatabaseProvider, Is.EqualTo(original.DatabaseProvider));
    }

    [Test]
    public void SaveThenLoad_NullProperties_RoundTripsCorrectly()
    {
        // Arrange
        var original = new UserSettings();

        // Act
        _sut.Save(original);
        var loaded = _sut.Load();

        // Assert
        Assert.That(loaded.DatabasePath, Is.Null);
        Assert.That(loaded.DatabaseProvider, Is.Null);
    }

    [Test]
    public void SaveThenLoad_OverwritesExistingFile()
    {
        // Arrange
        var first = new UserSettings { DatabaseProvider = DatabaseProvider.SQLite };
        var second = new UserSettings { DatabaseProvider = DatabaseProvider.InMemory };

        // Act
        _sut.Save(first);
        _sut.Save(second);
        var loaded = _sut.Load();

        // Assert
        Assert.That(loaded.DatabaseProvider, Is.EqualTo(DatabaseProvider.InMemory));
    }

    [Test]
    public void SaveThenLoad_EnabledCrawlerSteps_RoundTripsCorrectly()
    {
        // Arrange
        var original = new UserSettings
        {
            EnabledCrawlerSteps = ["Select1Month", "Select3Years", "SelectMax"]
        };

        // Act
        _sut.Save(original);
        var loaded = _sut.Load();

        // Assert
        Assert.That(loaded.EnabledCrawlerSteps, Is.Not.Null);
        Assert.That(loaded.EnabledCrawlerSteps, Is.EqualTo(original.EnabledCrawlerSteps));
    }

    [Test]
    public void Load_OldJsonWithoutEnabledCrawlerSteps_ReturnsNull()
    {
        // Arrange — simulate a settings.json from before this feature
        var json = """{ "databaseProvider": "SQLite", "databasePath": "funds.db" }""";
        File.WriteAllText(_settingsFilePath, json);

        // Act
        var loaded = _sut.Load();

        // Assert
        Assert.That(loaded.EnabledCrawlerSteps, Is.Null);
    }

    #endregion
}
