using CommandLine;
using NUnit.Framework;
using YieldRaccoon.Wpf.Configuration;

namespace YieldRaccoon.Wpf.Tests.Configuration;

[TestFixture]
[TestOf(typeof(AutoStartOptions))]
public class AutoStartOptionsTests
{
    #region CLI Parsing (integration with CommandLineParser)

    private static AutoStartOptions ParseArgs(params string[] args)
    {
        return Parser.Default
            .ParseArguments<AutoStartOptions>(args)
            .MapResult(opts => opts, _ => AutoStartOptions.None);
    }

    [Test]
    public void Parse_NoArgs_ReturnsDefaults()
    {
        // Act
        var result = ParseArgs();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.AutoList, Is.False);
            Assert.That(result.AutoOverview, Is.False);
            Assert.That(result.OverviewFundCount, Is.EqualTo(80));
            Assert.That(result.IsAnyAutoModeActive, Is.False);
        });
    }

    [Test]
    public void Parse_AutoListOnly_SetsAutoList()
    {
        // Act
        var result = ParseArgs("--auto-list");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.AutoList, Is.True);
            Assert.That(result.AutoOverview, Is.False);
        });
    }

    [Test]
    public void Parse_AutoOverviewWithCount_SetsAutoOverviewAndCount()
    {
        // Act
        var result = ParseArgs("--auto-overview", "50");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.AutoOverview, Is.True);
            Assert.That(result.AutoOverviewFundCount, Is.EqualTo(50));
            Assert.That(result.OverviewFundCount, Is.EqualTo(50));
        });
    }

    [Test]
    public void Parse_BothFlags_SetsBoth()
    {
        // Act
        var result = ParseArgs("--auto-list", "--auto-overview", "30");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.AutoList, Is.True);
            Assert.That(result.AutoOverview, Is.True);
            Assert.That(result.OverviewFundCount, Is.EqualTo(30));
        });
    }

    [Test]
    public void Parse_UnknownArgs_FallsBackToNone()
    {
        // Act — CommandLineParser fails on unknown args, MapResult returns None
        var result = ParseArgs("--unknown-flag");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.AutoList, Is.False);
            Assert.That(result.AutoOverview, Is.False);
        });
    }

    #endregion

    #region Computed Properties

    [Test]
    public void AutoOverview_WhenFundCountSet_ReturnsTrue()
    {
        // Arrange
        var options = new AutoStartOptions { AutoOverviewFundCount = 42 };

        // Assert
        Assert.That(options.AutoOverview, Is.True);
    }

    [Test]
    public void AutoOverview_WhenNull_ReturnsFalse()
    {
        // Arrange
        var options = new AutoStartOptions { AutoOverviewFundCount = null };

        // Assert
        Assert.That(options.AutoOverview, Is.False);
    }

    [Test]
    public void OverviewFundCount_WhenNull_DefaultsTo80()
    {
        // Arrange
        var options = new AutoStartOptions { AutoOverviewFundCount = null };

        // Assert
        Assert.That(options.OverviewFundCount, Is.EqualTo(80));
    }

    [Test]
    public void IsAnyAutoModeActive_NoFlags_ReturnsFalse()
    {
        // Arrange
        var options = new AutoStartOptions();

        // Assert
        Assert.That(options.IsAnyAutoModeActive, Is.False);
    }

    [Test]
    public void IsAnyAutoModeActive_AutoListOnly_ReturnsTrue()
    {
        // Arrange
        var options = new AutoStartOptions { AutoList = true };

        // Assert
        Assert.That(options.IsAnyAutoModeActive, Is.True);
    }

    [Test]
    public void IsAnyAutoModeActive_AutoOverviewOnly_ReturnsTrue()
    {
        // Arrange
        var options = new AutoStartOptions { AutoOverviewFundCount = 10 };

        // Assert
        Assert.That(options.IsAnyAutoModeActive, Is.True);
    }

    #endregion

    #region Static Members

    [Test]
    public void None_ReturnsDefaultInstance()
    {
        // Act
        var none = AutoStartOptions.None;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(none.AutoList, Is.False);
            Assert.That(none.AutoOverview, Is.False);
            Assert.That(none.OverviewFundCount, Is.EqualTo(80));
            Assert.That(none.IsAnyAutoModeActive, Is.False);
        });
    }

    #endregion
}
