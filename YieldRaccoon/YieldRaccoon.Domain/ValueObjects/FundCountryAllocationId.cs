using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for <see cref="Entities.FundCountryAllocation"/>.
/// </summary>
[DebuggerDisplay("FundCountryAllocationId: {Value}")]
public readonly record struct FundCountryAllocationId(Guid Value) : IComparable<FundCountryAllocationId>
{
    /// <summary>
    /// Creates a <see cref="FundCountryAllocationId"/> from an existing GUID.
    /// </summary>
    public static FundCountryAllocationId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(value), "Fund country allocation ID must not be empty.");

        return new FundCountryAllocationId(value);
    }

    /// <summary>
    /// Generates a fresh <see cref="FundCountryAllocationId"/> for a new allocation row.
    /// </summary>
    public static FundCountryAllocationId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public int CompareTo(FundCountryAllocationId other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Implicit conversion to <see cref="Guid"/>.
    /// </summary>
    public static implicit operator Guid(FundCountryAllocationId id) => id.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
