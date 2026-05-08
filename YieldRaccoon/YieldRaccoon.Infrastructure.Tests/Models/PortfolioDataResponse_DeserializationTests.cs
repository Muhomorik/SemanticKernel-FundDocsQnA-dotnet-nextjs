using System.Text.Json;
using NUnit.Framework;
using YieldRaccoon.Infrastructure.Models;

namespace YieldRaccoon.Infrastructure.Tests.Models;

[TestFixture]
[TestOf(typeof(PortfolioDataResponse))]
public class PortfolioDataResponse_DeserializationTests
{
    private const string TrimmedPayload = """
        {
          "countryChartData": [
            { "name": "USA", "y": 36.93, "countryCode": "US" },
            { "name": "Kanada", "y": 9.37, "countryCode": "CA" }
          ],
          "sectorChartData": [
            { "name": "Teknik", "y": 46.93 },
            { "name": "Råvaror", "y": 35.92 }
          ]
        }
        """;

    private const string FullPayloadWithHoldings = """
        {
          "countryChartData": [
            { "name": "USA", "y": 36.93, "countryCode": "US", "previousY": 0.0 }
          ],
          "holdingChartData": [
            { "name": "Some Holding", "y": 4.3 }
          ],
          "sectorChartData": [
            { "name": "Teknik", "y": 46.93, "previousY": 0.0 }
          ],
          "portfolioDate": "2026-03-31",
          "previousPortfolioDate": null
        }
        """;

    [Test]
    public void Deserialize_TrimmedPayload_BindsNameToDisplayNameAndYToPercentage()
    {
        // Arrange + Act
        var result = JsonSerializer.Deserialize<PortfolioDataResponse>(TrimmedPayload);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CountryChartData, Has.Count.EqualTo(2));
        Assert.That(result.CountryChartData![0].DisplayName, Is.EqualTo("USA"));
        Assert.That(result.CountryChartData[0].Percentage, Is.EqualTo(36.93));
        Assert.That(result.CountryChartData[0].CountryCode, Is.EqualTo("US"));

        Assert.That(result.SectorChartData, Has.Count.EqualTo(2));
        Assert.That(result.SectorChartData![0].DisplayName, Is.EqualTo("Teknik"));
        Assert.That(result.SectorChartData[0].Percentage, Is.EqualTo(46.93));
    }

    [Test]
    public void Deserialize_FullPayloadWithHoldings_IgnoresUnknownFields()
    {
        // Arrange + Act
        var result = JsonSerializer.Deserialize<PortfolioDataResponse>(FullPayloadWithHoldings);

        // Assert — holdings, previousY, portfolioDate are silently dropped
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CountryChartData, Has.Count.EqualTo(1));
        Assert.That(result.SectorChartData, Has.Count.EqualTo(1));
    }

    [Test]
    public void Deserialize_EmptyArrays_ReturnsEmptyCollections()
    {
        // Arrange
        var json = """{ "countryChartData": [], "sectorChartData": [] }""";

        // Act
        var result = JsonSerializer.Deserialize<PortfolioDataResponse>(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CountryChartData, Is.Empty);
        Assert.That(result.SectorChartData, Is.Empty);
    }

    [Test]
    public void Deserialize_CountryItemWithNullCode_PreservesNull()
    {
        // Arrange
        var json = """
            {
              "countryChartData": [{ "name": "Unknown", "y": 1.0, "countryCode": null }],
              "sectorChartData": []
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<PortfolioDataResponse>(json);

        // Assert
        Assert.That(result!.CountryChartData![0].CountryCode, Is.Null);
    }
}
