using NUnit.Framework;
using YieldRaccoon.Wpf.Configuration;

namespace YieldRaccoon.Wpf.Tests.Configuration;

[TestFixture]
[TestOf(typeof(IsoWeekFilenameBuilder))]
public class IsoWeekFilenameBuilderTests
{
    [Test]
    public void BuildFamilyTag_NullCompanyName_ReturnsAll()
    {
        Assert.That(IsoWeekFilenameBuilder.BuildFamilyTag(null), Is.EqualTo("all"));
    }

    [Test]
    public void BuildFamilyTag_EmptyCompanyName_ReturnsAll()
    {
        Assert.That(IsoWeekFilenameBuilder.BuildFamilyTag(string.Empty), Is.EqualTo("all"));
        Assert.That(IsoWeekFilenameBuilder.BuildFamilyTag("   "), Is.EqualTo("all"));
    }

    [Test]
    public void BuildFamilyTag_LowerCasesAndSanitizes()
    {
        Assert.That(IsoWeekFilenameBuilder.BuildFamilyTag("Schroder"), Is.EqualTo("schroder"));
        Assert.That(IsoWeekFilenameBuilder.BuildFamilyTag("My Asset Mgmt"), Is.EqualTo("my_asset_mgmt"));
    }

    [Test]
    public void BuildFamilyTag_ReplacesInvalidPathChars()
    {
        // ':' and '/' are invalid in Windows filenames
        var result = IsoWeekFilenameBuilder.BuildFamilyTag("Foo/Bar:Baz");
        Assert.That(result, Does.Not.Contain("/"));
        Assert.That(result, Does.Not.Contain(":"));
    }

    [Test]
    public void BuildIsoWeekTag_FormatsAsYYYY_Www()
    {
        // 2026-04-30 (Thursday) is in ISO week 2026-W18
        var when = new DateTime(2026, 4, 30);
        Assert.That(IsoWeekFilenameBuilder.BuildIsoWeekTag(when), Is.EqualTo("2026-W18"));
    }

    [Test]
    public void BuildIsoWeekTag_HandlesEarlyJanuaryAsPriorYearW53()
    {
        // 2027-01-01 is a Friday — ISO week 2026-W53.
        // (Verifies we use ISO week-year, not calendar year, in the prefix.)
        var when = new DateTime(2027, 1, 1);
        Assert.That(IsoWeekFilenameBuilder.BuildIsoWeekTag(when), Is.EqualTo("2026-W53"));
    }

    [Test]
    public void BuildIsoWeekTag_HandlesLateDecemberAsNextYearW01()
    {
        // 2024-12-30 (Monday) is in ISO week 2025-W01 — bumps to next ISO week-year.
        var when = new DateTime(2024, 12, 30);
        Assert.That(IsoWeekFilenameBuilder.BuildIsoWeekTag(when), Is.EqualTo("2025-W01"));
    }
}
