using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for <see cref="Entities.FundSectorAllocation"/>.
/// </summary>
[DebuggerDisplay("FundSectorAllocationId: {Value}")]
public readonly record struct FundSectorAllocationId(Guid Value) : IComparable<FundSectorAllocationId>
{
    /// <summary>
    /// Creates a <see cref="FundSectorAllocationId"/> from an existing GUID.
    /// </summary>
    public static FundSectorAllocationId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(value), "Fund sector allocation ID must not be empty.");

        return new FundSectorAllocationId(value);
    }

    /// <summary>
    /// Generates a fresh <see cref="FundSectorAllocationId"/> for a new allocation row.
    /// </summary>
    public static FundSectorAllocationId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public int CompareTo(FundSectorAllocationId other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Implicit conversion to <see cref="Guid"/>.
    /// </summary>
    public static implicit operator Guid(FundSectorAllocationId id) => id.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
