using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Represents a batch number for "Visa fler" clicks during fund list crawling.
/// Each batch loads approximately 20 funds.
/// </summary>
/// <remarks>
/// <para>
/// Batch numbers are 1-based. Batch 1 is the first click on "Visa fler" button
/// (the initial 20 funds are auto-loaded, not counted as a batch).
/// </para>
/// </remarks>
[DebuggerDisplay("Batch {Value}")]
public readonly record struct FundListBatchNumber : IComparable<FundListBatchNumber>
{
    /// <summary>
    /// Gets the batch number value (1-based).
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="FundListBatchNumber"/>.
    /// </summary>
    /// <param name="value">The batch number (must be positive).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than 1.</exception>
    public FundListBatchNumber(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Batch number must be positive (1-based).");
        }

        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="FundListBatchNumber"/> with the specified value.
    /// </summary>
    /// <param name="value">The batch number (must be positive).</param>
    /// <returns>A new <see cref="FundListBatchNumber"/> instance.</returns>
    public static FundListBatchNumber Create(int value) => new(value);

    /// <summary>
    /// Compares this batch number to another.
    /// </summary>
    public int CompareTo(FundListBatchNumber other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Returns a string representation of the batch number.
    /// </summary>
    public override string ToString() => $"Batch {Value}";

    /// <summary>
    /// Implicitly converts a <see cref="FundListBatchNumber"/> to its integer value.
    /// </summary>
    public static implicit operator int(FundListBatchNumber batchNumber) => batchNumber.Value;
}
