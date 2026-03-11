using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.Tests.Domain.FundData;

[TestFixture]
[Category("Unit")]
[Category("FundData")]
public class CategoryMacroGroupTests
{
    [TestCase("Aktiefond Sverige", "Sverige")]
    [TestCase("Indexfond Sverige", "Sverige")]
    [TestCase("Aktier USA", "USA")]
    [TestCase("Aktiefond USA", "USA")]
    [TestCase("Aktiefond Europa", "Europa")]
    [TestCase("Aktiefond Finland", "Europa")]
    [TestCase("Aktiefond Global", "Global")]
    [TestCase("Globalfond", "Global")]
    [TestCase("Tillvaxtmarknader", "Emerging Markets")]
    [TestCase("Aktiefond Indien", "Emerging Markets")]
    [TestCase("Aktiefond Kina", "Emerging Markets")]
    [TestCase("Aktiefond Japan", "Japan/Asia")]
    [TestCase("Aktiefond Asien", "Japan/Asia")]
    [TestCase("Branschfond Teknik", "Sector Funds")]
    [TestCase("Rante - SEK Kort", "SEK Bonds")]
    [TestCase("Rante - euro", "Euro Bonds")]
    [TestCase("Blandfond Forsiktig", "Mixed Funds")]
    public void Resolve_KnownCategory_ReturnsMacroGroup(string category, string expected)
    {
        Assert.That(CategoryMacroGroup.Resolve(category), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Resolve_NullOrEmpty_ReturnsOther(string? category)
    {
        Assert.That(CategoryMacroGroup.Resolve(category), Is.EqualTo("Other"));
    }

    [TestCase("Unknown Category")]
    [TestCase("Hedgefond")]
    [TestCase("Private Equity")]
    public void Resolve_UnmappedCategory_ReturnsOther(string category)
    {
        Assert.That(CategoryMacroGroup.Resolve(category), Is.EqualTo("Other"));
    }

    [Test]
    public void Resolve_CaseInsensitive_MatchesCorrectly()
    {
        Assert.That(CategoryMacroGroup.Resolve("aktiefond GLOBAL"), Is.EqualTo("Global"));
        Assert.That(CategoryMacroGroup.Resolve("RANTE - SEK"), Is.EqualTo("SEK Bonds"));
    }

    [Test]
    public void Resolve_BondPatternsMatchBeforeRegions()
    {
        // "Rante - SEK" contains no region keywords, but verify bonds don't accidentally
        // match a region pattern if one were to overlap
        Assert.That(CategoryMacroGroup.Resolve("Rante - SEK"), Is.EqualTo("SEK Bonds"));
        Assert.That(CategoryMacroGroup.Resolve("Rante - euro"), Is.EqualTo("Euro Bonds"));
    }
}
