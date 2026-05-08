using System.Diagnostics;

namespace Backend.API.Domain.FundData.ValueObjects;

/// <summary>
/// Strongly-typed identifier for <c>FundSectorAllocation</c>.
/// </summary>
[DebuggerDisplay("FundSectorAllocationId: {Value}")]
public readonly record struct FundSectorAllocationId(Guid Value)
{
    public static FundSectorAllocationId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(value), "Fund sector allocation ID must not be empty.");
        return new FundSectorAllocationId(value);
    }

    public static FundSectorAllocationId New() => new(Guid.NewGuid());

    public static implicit operator Guid(FundSectorAllocationId id) => id.Value;
}
