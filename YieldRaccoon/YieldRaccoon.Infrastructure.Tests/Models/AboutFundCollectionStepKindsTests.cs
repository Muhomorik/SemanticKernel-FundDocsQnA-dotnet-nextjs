using NUnit.Framework;
using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Infrastructure.Tests.Models;

[TestFixture]
[TestOf(typeof(AboutFundCollectionStepKinds))]
public class AboutFundCollectionStepKindsTests
{
    [Test]
    public void Defaults_ContainsExpectedSteps()
    {
        // Assert
        Assert.That(AboutFundCollectionStepKinds.Defaults, Does.Contain(AboutFundCollectionStepKind.Select1Month));
        Assert.That(AboutFundCollectionStepKinds.Defaults, Does.Contain(AboutFundCollectionStepKind.Select3Months));
        Assert.That(AboutFundCollectionStepKinds.Defaults, Does.Contain(AboutFundCollectionStepKind.SelectYearToDate));
        Assert.That(AboutFundCollectionStepKinds.Defaults, Does.Contain(AboutFundCollectionStepKind.Select1Year));
        Assert.That(AboutFundCollectionStepKinds.Defaults, Does.Contain(AboutFundCollectionStepKind.Select3Years));
        Assert.That(AboutFundCollectionStepKinds.Defaults, Does.Contain(AboutFundCollectionStepKind.Select5Years));
        Assert.That(AboutFundCollectionStepKinds.Defaults, Does.Contain(AboutFundCollectionStepKind.SelectMax));
    }

    [Test]
    public void Defaults_DoesNotContainDisabledSteps()
    {
        // Assert
        Assert.That(AboutFundCollectionStepKinds.Defaults, Does.Not.Contain(AboutFundCollectionStepKind.ActivateSekView));
    }

    [Test]
    public void Configurable_ExcludesActivateSekView()
    {
        // Assert
        Assert.That(AboutFundCollectionStepKinds.Configurable,
            Does.Not.Contain(AboutFundCollectionStepKind.ActivateSekView));
    }

    [Test]
    public void Configurable_ContainsAll7PeriodSteps()
    {
        // Assert
        Assert.That(AboutFundCollectionStepKinds.Configurable, Has.Count.EqualTo(7));
        Assert.That(AboutFundCollectionStepKinds.Configurable, Does.Contain(AboutFundCollectionStepKind.Select1Month));
        Assert.That(AboutFundCollectionStepKinds.Configurable, Does.Contain(AboutFundCollectionStepKind.SelectMax));
    }

    [Test]
    public void ForSteps_EmptyInput_ReturnsOnlyActivateSekView()
    {
        // Act
        var result = AboutFundCollectionStepKinds.ForSteps([]);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(AboutFundCollectionStepKind.ActivateSekView));
    }

    [Test]
    public void ForSteps_SubsetOfSteps_AlwaysPrependsActivateSekView()
    {
        // Arrange
        var enabled = new[] { AboutFundCollectionStepKind.Select1Month };

        // Act
        var result = AboutFundCollectionStepKinds.ForSteps(enabled);

        // Assert
        Assert.That(result[0], Is.EqualTo(AboutFundCollectionStepKind.ActivateSekView));
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void ForSteps_SubsetOfSteps_PreservesCanonicalOrder()
    {
        // Arrange — pass in reverse order
        var enabled = new[]
        {
            AboutFundCollectionStepKind.SelectMax,
            AboutFundCollectionStepKind.Select1Month,
            AboutFundCollectionStepKind.Select3Years
        };

        // Act
        var result = AboutFundCollectionStepKinds.ForSteps(enabled);

        // Assert — should be in canonical order, not input order
        Assert.That(result, Is.EqualTo(new[]
        {
            AboutFundCollectionStepKind.ActivateSekView,
            AboutFundCollectionStepKind.Select1Month,
            AboutFundCollectionStepKind.Select3Years,
            AboutFundCollectionStepKind.SelectMax
        }));
    }

    [Test]
    public void ForSteps_AllConfigurable_MatchesAll()
    {
        // Act
        var result = AboutFundCollectionStepKinds.ForSteps(AboutFundCollectionStepKinds.Configurable);

        // Assert
        Assert.That(result, Is.EqualTo(AboutFundCollectionStepKinds.All));
    }

    [Test]
    public void ForSteps_IgnoresActivateSekViewInInput()
    {
        // Arrange — include ActivateSekView in input (should not duplicate)
        var enabled = new[]
        {
            AboutFundCollectionStepKind.ActivateSekView,
            AboutFundCollectionStepKind.Select1Month
        };

        // Act
        var result = AboutFundCollectionStepKinds.ForSteps(enabled);

        // Assert — ActivateSekView appears exactly once
        Assert.That(result.Count(s => s == AboutFundCollectionStepKind.ActivateSekView), Is.EqualTo(1));
        Assert.That(result, Has.Count.EqualTo(2));
    }

    #region FromNames

    [Test]
    public void FromNames_Null_ReturnsDefaults()
    {
        // Act
        var result = AboutFundCollectionStepKinds.FromNames(null);

        // Assert
        Assert.That(result, Is.EqualTo(AboutFundCollectionStepKinds.Defaults));
    }

    [Test]
    public void FromNames_EmptyList_ReturnsDefaults()
    {
        // Act
        var result = AboutFundCollectionStepKinds.FromNames([]);

        // Assert
        Assert.That(result, Is.EqualTo(AboutFundCollectionStepKinds.Defaults));
    }

    [Test]
    public void FromNames_ValidNames_ReturnsParsedSet()
    {
        // Arrange
        var names = new[] { "Select1Month", "Select3Years" };

        // Act
        var result = AboutFundCollectionStepKinds.FromNames(names);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Does.Contain(AboutFundCollectionStepKind.Select1Month));
        Assert.That(result, Does.Contain(AboutFundCollectionStepKind.Select3Years));
    }

    [Test]
    public void FromNames_UnknownNames_Ignored()
    {
        // Arrange
        var names = new[] { "Select1Month", "FutureStep", "InvalidName" };

        // Act
        var result = AboutFundCollectionStepKinds.FromNames(names);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Does.Contain(AboutFundCollectionStepKind.Select1Month));
    }

    [Test]
    public void FromNames_ActivateSekView_Ignored()
    {
        // Arrange
        var names = new[] { "ActivateSekView", "Select1Month" };

        // Act
        var result = AboutFundCollectionStepKinds.FromNames(names);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Does.Contain(AboutFundCollectionStepKind.Select1Month));
        Assert.That(result, Does.Not.Contain(AboutFundCollectionStepKind.ActivateSekView));
    }

    #endregion

    #region ToNames

    [Test]
    public void ToNames_AllDefaults_ReturnsNull()
    {
        // Act
        var result = AboutFundCollectionStepKinds.ToNames(AboutFundCollectionStepKinds.Defaults);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ToNames_Subset_ReturnsNamesInCanonicalOrder()
    {
        // Arrange — pass in reverse order
        var steps = new[]
        {
            AboutFundCollectionStepKind.SelectMax,
            AboutFundCollectionStepKind.Select1Month
        };

        // Act
        var result = AboutFundCollectionStepKinds.ToNames(steps);

        // Assert — canonical order preserved
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(new[] { "Select1Month", "SelectMax" }));
    }

    [Test]
    public void ToNames_IgnoresActivateSekView()
    {
        // Arrange — include ActivateSekView (should be filtered out)
        var steps = new[]
        {
            AboutFundCollectionStepKind.ActivateSekView,
            AboutFundCollectionStepKind.Select1Month
        };

        // Act
        var result = AboutFundCollectionStepKinds.ToNames(steps);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(new[] { "Select1Month" }));
    }

    [Test]
    public void RoundTrip_SubsetPreserved()
    {
        // Arrange
        var original = new[]
        {
            AboutFundCollectionStepKind.Select1Month,
            AboutFundCollectionStepKind.Select3Years,
            AboutFundCollectionStepKind.SelectMax
        };

        // Act
        var names = AboutFundCollectionStepKinds.ToNames(original);
        var roundTripped = AboutFundCollectionStepKinds.FromNames(names);

        // Assert
        Assert.That(roundTripped, Is.EquivalentTo(original));
    }

    #endregion
}
