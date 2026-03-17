using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Mappers;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;

namespace YieldRaccoon.Infrastructure.Tests.Mappers;

[TestFixture]
[TestOf(typeof(ApiFundListDtoMapper))]
public class ApiFundListDtoMapperTests
{
    private IFixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());
    }

    [Test]
    public void ToApiFundListDto_FundListDataDto_MapsAllProperties()
    {
        // Arrange
        var dto = new FundListDataDto
        {
            Isin = "SE0001234567",
            Name = "Test Fund",
            OrderbookId = "123456",
            Category = "Equity",
            CompanyName = "Test Company",
            FundType = "Equity Fund",
            IsIndexFund = true,
            StartDate = "2020-01-01",
            CurrencyCode = "SEK",
            ManagedType = "Active",
            Buyable = true,
            HasCashDividends = false,
            HasCurrencyExchangeFee = true,
            RecommendedHoldingPeriod = "5 years",
            ManagementFee = 1.5m,
            TotalFee = 1.8m,
            TransactionFee = 0.1m,
            OngoingFee = 1.6m,
            MinimumBuy = 100m,
            Nav = 150.25m,
            NavDate = "2024-01-15",
            Capital = 1_000_000m,
            NumberOfOwners = 5000,
            Rating = 4,
            Risk = 5,
            SharpeRatio = 1.2m,
            StandardDeviation = 15.3m,
            SustainabilityLevel = "High",
            SustainabilityRating = 4,
            EsgScore = 8.5m,
            EnvironmentalScore = 9.0m,
            SocialScore = 7.5m,
            GovernanceScore = 8.0m,
            LowCarbon = true,
            EuArticleType = "Article 8"
        };

        // Act
        var result = dto.ToApiFundListDto();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Isin, Is.EqualTo("SE0001234567"));
            Assert.That(result.Name, Is.EqualTo("Test Fund"));
            Assert.That(result.OrderbookId, Is.EqualTo("123456"));
            Assert.That(result.Category, Is.EqualTo("Equity"));
            Assert.That(result.CompanyName, Is.EqualTo("Test Company"));
            Assert.That(result.FundType, Is.EqualTo("Equity Fund"));
            Assert.That(result.IsIndexFund, Is.True);
            Assert.That(result.StartDate, Is.EqualTo("2020-01-01"));
            Assert.That(result.CurrencyCode, Is.EqualTo("SEK"));
            Assert.That(result.ManagedType, Is.EqualTo("Active"));
            Assert.That(result.Buyable, Is.True);
            Assert.That(result.HasCashDividends, Is.False);
            Assert.That(result.HasCurrencyExchangeFee, Is.True);
            Assert.That(result.ManagementFee, Is.EqualTo(1.5m));
            Assert.That(result.TotalFee, Is.EqualTo(1.8m));
            Assert.That(result.TransactionFee, Is.EqualTo(0.1m));
            Assert.That(result.OngoingFee, Is.EqualTo(1.6m));
            Assert.That(result.MinimumBuy, Is.EqualTo(100m));
            Assert.That(result.Nav, Is.EqualTo(150.25m));
            Assert.That(result.NavDate, Is.EqualTo("2024-01-15"));
            Assert.That(result.Capital, Is.EqualTo(1_000_000m));
            Assert.That(result.NumberOfOwners, Is.EqualTo(5000));
            Assert.That(result.Rating, Is.EqualTo(4));
            Assert.That(result.Risk, Is.EqualTo(5));
            Assert.That(result.SharpeRatio, Is.EqualTo(1.2m));
            Assert.That(result.StandardDeviation, Is.EqualTo(15.3m));
            Assert.That(result.EsgScore, Is.EqualTo(8.5m));
            Assert.That(result.LowCarbon, Is.True);
            Assert.That(result.EuArticleType, Is.EqualTo("Article 8"));
        });
    }

    [Test]
    public void ToApiFundListDto_FundProfile_MapsIsinIdToString()
    {
        // Arrange
        var isin = "SE0009999999";
        var profile = new FundProfile
        {
            Id = new IsinId(isin),
            Name = "Profile Fund",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = profile.ToApiFundListDto();

        // Assert
        Assert.That(result.Isin, Is.EqualTo(isin));
        Assert.That(result.Name, Is.EqualTo("Profile Fund"));
    }

    [Test]
    public void ToApiFundListDto_FundProfile_MapsDateOnlyToString()
    {
        // Arrange
        var profile = new FundProfile
        {
            Id = new IsinId("SE0001111111"),
            Name = "Date Fund",
            FirstSeenAt = DateTimeOffset.UtcNow,
            StartDate = new DateOnly(2020, 6, 15)
        };

        // Act
        var result = profile.ToApiFundListDto();

        // Assert
        Assert.That(result.StartDate, Is.EqualTo("2020-06-15"));
    }

    [Test]
    public void ToApiFundListDto_FundProfile_NullStartDate_MapsToNull()
    {
        // Arrange
        var profile = new FundProfile
        {
            Id = new IsinId("SE0002222222"),
            Name = "No Date Fund",
            FirstSeenAt = DateTimeOffset.UtcNow,
            StartDate = null
        };

        // Act
        var result = profile.ToApiFundListDto();

        // Assert
        Assert.That(result.StartDate, Is.Null);
    }

    [Test]
    public void ToApiFundListDtos_FiltersNullIsinAndName()
    {
        // Arrange
        var dtos = new List<FundListDataDto>
        {
            new() { Isin = "SE0001234567", Name = "Valid" },
            new() { Isin = null, Name = "No ISIN" },
            new() { Isin = "SE0009876543", Name = null },
            new() { Isin = "", Name = "Empty ISIN" },
            new() { Isin = "SE0005555555", Name = " " },
            new() { Isin = "SE0003333333", Name = "Also Valid" }
        };

        // Act
        var result = dtos.ToApiFundListDtos();

        // Assert — only entries with both non-null/non-whitespace ISIN and Name
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(f => f.Isin), Is.EquivalentTo(new[] { "SE0001234567", "SE0003333333" }));
    }
}
