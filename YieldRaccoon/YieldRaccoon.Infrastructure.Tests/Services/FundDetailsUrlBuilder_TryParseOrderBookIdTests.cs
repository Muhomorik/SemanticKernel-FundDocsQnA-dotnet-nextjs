using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;

namespace YieldRaccoon.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="FundDetailsUrlBuilder.TryParseOrderBookId"/>.
/// </summary>
[TestFixture]
[TestOf(typeof(FundDetailsUrlBuilder))]
public class FundDetailsUrlBuilder_TryParseOrderBookIdTests
{
    private IFixture _fixture = null!;

    private const string TemplateWithSuffix = "https://www.example.com/fonder/{0}/about";
    private const string TemplateWithoutSuffix = "https://www.example.com/fonder/{0}";

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());
    }

    #region Valid URLs

    [Test]
    public void TryParseOrderBookId_ValidFundUrl_ReturnsTrueAndCorrectOrderBookId()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithSuffix);

        // Act
        var result = sut.TryParseOrderBookId(new Uri("https://www.example.com/fonder/325410/about"), out var orderBookId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(orderBookId.Value, Is.EqualTo("325410"));
    }

    [Test]
    public void TryParseOrderBookId_UrlWithQueryParams_ExtractsCorrectly()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithSuffix);

        // Act — query params come after the suffix
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.example.com/fonder/325410/about?tab=chart"), out var orderBookId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(orderBookId.Value, Is.EqualTo("325410"));
    }

    [Test]
    public void TryParseOrderBookId_TemplateWithoutSuffix_ExtractsOrderBookId()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithoutSuffix);

        // Act
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.example.com/fonder/99887"), out var orderBookId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(orderBookId.Value, Is.EqualTo("99887"));
    }

    [Test]
    public void TryParseOrderBookId_TemplateWithoutSuffix_UrlWithQueryParams_StopsBeforeQuery()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithoutSuffix);

        // Act
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.example.com/fonder/99887?tab=chart"), out var orderBookId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(orderBookId.Value, Is.EqualTo("99887"));
    }

    [Test]
    public void TryParseOrderBookId_TemplateWithoutSuffix_UrlWithFragment_StopsBeforeFragment()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithoutSuffix);

        // Act
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.example.com/fonder/99887#section"), out var orderBookId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(orderBookId.Value, Is.EqualTo("99887"));
    }

    [Test]
    public void TryParseOrderBookId_RoundTrip_ParsesWhatBuildUrlProduces()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithSuffix);
        var originalId = _fixture.Create<OrderBookId>();
        var url = sut.BuildUrl(originalId);

        // Act
        var result = sut.TryParseOrderBookId(url, out var parsedId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(parsedId, Is.EqualTo(originalId));
    }

    #endregion

    #region Invalid URLs

    [Test]
    public void TryParseOrderBookId_DifferentDomain_ReturnsFalse()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithSuffix);

        // Act
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.other-site.com/fonder/325410/about"), out _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParseOrderBookId_DifferentPath_ReturnsFalse()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithSuffix);

        // Act
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.example.com/other/325410/about"), out _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParseOrderBookId_MissingSuffix_ReturnsFalse()
    {
        // Arrange — template expects "/about" after the ID
        var sut = CreateBuilder(TemplateWithSuffix);

        // Act — URL has the ID but no "/about" suffix
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.example.com/fonder/325410"), out _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParseOrderBookId_EmptyOrderBookIdSegment_ReturnsFalse()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithSuffix);

        // Act — empty segment between prefix and suffix
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.example.com/fonder//about"), out _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParseOrderBookId_TemplateWithoutSuffix_PrefixOnly_ReturnsFalse()
    {
        // Arrange
        var sut = CreateBuilder(TemplateWithoutSuffix);

        // Act — URL matches prefix exactly, no ID segment
        var result = sut.TryParseOrderBookId(
            new Uri("https://www.example.com/fonder/"), out _);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Helpers

    private static FundDetailsUrlBuilder CreateBuilder(string template)
    {
        return new FundDetailsUrlBuilder(new FundDetailsUrlBuilderOptions(template));
    }

    #endregion
}
