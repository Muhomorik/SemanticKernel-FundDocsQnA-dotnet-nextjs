using NUnit.Framework;
using YieldRaccoon.Infrastructure.Services;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(AllocationColumnSanitizer))]
public class AllocationColumnSanitizerTests
{
    [TestCase("USA", "usa")]
    [TestCase("Sverige", "sverige")]
    [TestCase("Storbritannien", "storbritannien")]
    [TestCase("Teknik", "teknik")]
    [TestCase("Industri", "industri")]
    public void Sanitize_PlainAsciiName_LowercasesIt(string input, string expected)
    {
        var result = AllocationColumnSanitizer.Sanitize(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Råvaror", "ravaror")]
    [TestCase("Hälsovård", "halsovard")]
    [TestCase("Tjeckien", "tjeckien")]
    [TestCase("Sydkorea", "sydkorea")]
    [TestCase("Élève", "eleve")]
    public void Sanitize_NameWithDiacritics_FoldsToAscii(string input, string expected)
    {
        var result = AllocationColumnSanitizer.Sanitize(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("North America", "north_america")]
    [TestCase("South Korea", "south_korea")]
    [TestCase("Latin  America", "latin_america")]
    public void Sanitize_NameWithWhitespace_ReplacesWithSingleUnderscore(string input, string expected)
    {
        var result = AllocationColumnSanitizer.Sanitize(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Telecom & Media", "telecom_media")]
    [TestCase("Health/Care", "health_care")]
    [TestCase("Financial-Services", "financial_services")]
    [TestCase("Tech (Hardware)", "tech_hardware")]
    public void Sanitize_NameWithSpecialChars_CollapsesToUnderscores(string input, string expected)
    {
        var result = AllocationColumnSanitizer.Sanitize(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Sanitize_NameWithLeadingAndTrailingSpaces_TrimsThem()
    {
        var result = AllocationColumnSanitizer.Sanitize("  Sverige  ");
        Assert.That(result, Is.EqualTo("sverige"));
    }

    [Test]
    public void Sanitize_NameWithTrailingSpecialChars_TrimsTrailingUnderscore()
    {
        var result = AllocationColumnSanitizer.Sanitize("Industri & Co.");
        Assert.That(result, Is.EqualTo("industri_co"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t")]
    public void Sanitize_NullOrBlank_Throws(string? input)
    {
        Assert.Throws<InvalidOperationException>(() => AllocationColumnSanitizer.Sanitize(input!));
    }

    [TestCase("…")]
    [TestCase("!!!")]
    [TestCase("---")]
    public void Sanitize_NonRepresentableName_Throws(string input)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => AllocationColumnSanitizer.Sanitize(input));
        Assert.That(ex!.Message, Does.Contain(input));
    }

    [Test]
    public void Sanitize_DigitsArePreserved()
    {
        var result = AllocationColumnSanitizer.Sanitize("Region 7");
        Assert.That(result, Is.EqualTo("region_7"));
    }
}
