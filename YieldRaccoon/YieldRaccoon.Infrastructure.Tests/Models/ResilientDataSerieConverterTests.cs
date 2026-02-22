using System.Text.Json;
using NUnit.Framework;
using YieldRaccoon.Infrastructure.Models;

namespace YieldRaccoon.Infrastructure.Tests.Models;

[TestFixture]
[TestOf(typeof(ResilientDataSerieConverter))]
public class ResilientDataSerieConverterTests
{
    [Test]
    public void Deserialize_MalformedYAsObject_SkipsMalformedKeepsValid()
    {
        // Arrange — exact payload from the bug report
        const string json = """
        {
          "id": "100",
          "dataSerie": [
            {
              "x": 1770678000000,
              "y": { "source": "465.0", "parsedValue": 465 }
            },
            {
              "x": 1771369200000,
              "y": 457.83
            }
          ],
          "name": "Fund with invalid data",
          "fromDate": "2026-01-19",
          "toDate": "2026-02-18"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<AboutFundChartResponse>(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.DataSerie, Is.Not.Null);
        Assert.That(result.DataSerie, Has.Count.EqualTo(1));
        Assert.That(result.DataSerie![0].X, Is.EqualTo(1771369200000L));
        Assert.That(result.DataSerie[0].Y, Is.EqualTo(457.83m));
    }

    [Test]
    public void Deserialize_AllValidPoints_ReturnsAll()
    {
        const string json = """
        {
          "id": "200",
          "dataSerie": [
            { "x": 1000, "y": 10.5 },
            { "x": 2000, "y": 20.75 },
            { "x": 3000, "y": 30.0 }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<AboutFundChartResponse>(json);

        Assert.That(result!.DataSerie, Has.Count.EqualTo(3));
    }

    [Test]
    public void Deserialize_AllMalformed_ReturnsEmptyList()
    {
        const string json = """
        {
          "id": "300",
          "dataSerie": [
            { "x": 1000, "y": { "source": "bad" } },
            { "x": 2000, "y": { "source": "also bad" } }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<AboutFundChartResponse>(json);

        Assert.That(result!.DataSerie, Is.Not.Null);
        Assert.That(result.DataSerie, Is.Empty);
    }

    [Test]
    public void Deserialize_NullDataSerie_ReturnsNull()
    {
        const string json = """
        {
          "id": "400",
          "dataSerie": null
        }
        """;

        var result = JsonSerializer.Deserialize<AboutFundChartResponse>(json);

        Assert.That(result!.DataSerie, Is.Null);
    }

    [Test]
    public void Deserialize_EmptyArray_ReturnsEmptyList()
    {
        const string json = """
        {
          "id": "500",
          "dataSerie": []
        }
        """;

        var result = JsonSerializer.Deserialize<AboutFundChartResponse>(json);

        Assert.That(result!.DataSerie, Is.Not.Null);
        Assert.That(result.DataSerie, Is.Empty);
    }

    [Test]
    public void Deserialize_MalformedYAsString_SkipsMalformedKeepsValid()
    {
        const string json = """
        {
          "id": "600",
          "dataSerie": [
            { "x": 1000, "y": "not a number" },
            { "x": 2000, "y": 42.0 }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<AboutFundChartResponse>(json);

        Assert.That(result!.DataSerie, Has.Count.EqualTo(1));
        Assert.That(result.DataSerie![0].X, Is.EqualTo(2000L));
        Assert.That(result.DataSerie[0].Y, Is.EqualTo(42.0m));
    }
}
