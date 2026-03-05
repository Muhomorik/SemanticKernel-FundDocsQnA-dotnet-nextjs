using Backend.API.Domain.FundData.ValueObjects;
using NUnit.Framework;

namespace Backend.Tests.Domain.FundData;

[TestFixture]
public class FundHistoryRecordIdTests
{
    [Test]
    public void Create_ValidPositiveValue_ReturnsId()
    {
        // Act
        var result = FundHistoryRecordId.Create(42);

        // Assert
        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Create_Zero_ReturnsId()
    {
        // Act
        var result = FundHistoryRecordId.Create(0);

        // Assert
        Assert.That(result.Value, Is.EqualTo(0));
    }

    [Test]
    public void Create_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => FundHistoryRecordId.Create(-1));
    }

    [Test]
    public void New_ReturnsIdWithZeroValue()
    {
        // Act
        var result = FundHistoryRecordId.New();

        // Assert
        Assert.That(result.Value, Is.EqualTo(0));
    }

    [Test]
    public void ImplicitConversion_ToLong_ReturnsValue()
    {
        // Arrange
        var id = FundHistoryRecordId.Create(99);

        // Act
        long value = id;

        // Assert
        Assert.That(value, Is.EqualTo(99));
    }

    [Test]
    public void CompareTo_SmallerValue_ReturnsNegative()
    {
        // Arrange
        var smaller = FundHistoryRecordId.Create(1);
        var larger = FundHistoryRecordId.Create(10);

        // Act
        var result = smaller.CompareTo(larger);

        // Assert
        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void ToString_ReturnsValueAsString()
    {
        // Arrange
        var id = FundHistoryRecordId.Create(42);

        // Act & Assert
        Assert.That(id.ToString(), Is.EqualTo("42"));
    }
}
