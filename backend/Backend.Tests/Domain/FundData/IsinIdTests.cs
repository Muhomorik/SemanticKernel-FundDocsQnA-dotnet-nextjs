using Backend.API.Domain.FundData.ValueObjects;
using NUnit.Framework;

namespace Backend.Tests.Domain.FundData;

[TestFixture]
[Category("Unit")]
[Category("FundData")]
public class IsinIdTests
{
    [TestCase("SE0008613939")]
    [TestCase("LU0274208692")]
    [TestCase("IE00B4L5Y983")]
    public void Create_ValidIsin_ReturnsIsinId(string isin)
    {
        // Act
        var result = IsinId.Create(isin);

        // Assert
        Assert.That(result.Isin, Is.EqualTo(isin));
    }

    [Test]
    public void Create_NullIsin_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => IsinId.Create(null!));
    }

    [Test]
    public void Create_EmptyIsin_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => IsinId.Create(""));
    }

    [Test]
    public void Create_WhitespaceIsin_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => IsinId.Create("   "));
    }

    [TestCase("se0008613939", Description = "Lowercase country code")]
    [TestCase("SE000861393", Description = "Too short (11 chars)")]
    [TestCase("SE00086139399", Description = "Too long (13 chars)")]
    [TestCase("123456789012", Description = "No country code")]
    [TestCase("SE0008613A3A", Description = "Letter in checksum position")]
    public void Create_InvalidFormat_ThrowsArgumentException(string isin)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => IsinId.Create(isin));
    }

    [Test]
    public void Parse_ValidIsin_ReturnsIsinId()
    {
        // Act
        var result = IsinId.Parse("SE0008613939");

        // Assert
        Assert.That(result.Isin, Is.EqualTo("SE0008613939"));
    }

    [Test]
    public void Equality_SameIsin_AreEqual()
    {
        // Arrange
        var id1 = IsinId.Create("SE0008613939");
        var id2 = IsinId.Create("SE0008613939");

        // Assert
        Assert.That(id1, Is.EqualTo(id2));
    }

    [Test]
    public void Equality_DifferentIsin_AreNotEqual()
    {
        // Arrange
        var id1 = IsinId.Create("SE0008613939");
        var id2 = IsinId.Create("LU0274208692");

        // Assert
        Assert.That(id1, Is.Not.EqualTo(id2));
    }
}
