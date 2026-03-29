using System.Text.Json;
using NUnit.Framework;
using YieldRaccoon.Infrastructure.Models;

namespace YieldRaccoon.Infrastructure.Tests.Models;

[TestFixture]
[TestOf(typeof(FundReferenceResponse))]
public class FundReferenceResponse_DeserializationTests
{
    private static readonly string TestDataPath = Path.Combine(
        AppContext.BaseDirectory, "TestData", "fund-reference-response.json");

    [Test]
    public void Deserialize_FixtureFile_ExtractsDescription()
    {
        // Arrange
        var json = File.ReadAllText(TestDataPath);

        // Act
        var result = JsonSerializer.Deserialize<FundReferenceResponse>(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Description, Is.EqualTo(
            "A diversified global equity fund investing across developed markets with focus on quality companies."));
    }

    [Test]
    public void Deserialize_NullDescription_ReturnsNull()
    {
        // Arrange
        var json = """{ "description": null, "name": "Test Fund" }""";

        // Act
        var result = JsonSerializer.Deserialize<FundReferenceResponse>(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Description, Is.Null);
    }

    [Test]
    public void Deserialize_MissingDescriptionField_ReturnsNull()
    {
        // Arrange
        var json = """{ "name": "Test Fund", "isin": "SE0000000001" }""";

        // Act
        var result = JsonSerializer.Deserialize<FundReferenceResponse>(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Description, Is.Null);
    }

    [Test]
    public void Deserialize_EmptyJson_ReturnsNullDescription()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = JsonSerializer.Deserialize<FundReferenceResponse>(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Description, Is.Null);
    }

    [Test]
    public void Deserialize_MalformedJson_ThrowsJsonException()
    {
        // Arrange
        var json = "not valid json";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<FundReferenceResponse>(json));
    }
}
