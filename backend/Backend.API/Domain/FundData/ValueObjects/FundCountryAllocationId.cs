using System.Diagnostics;

namespace Backend.API.Domain.FundData.ValueObjects;

/// <summary>
/// Strongly-typed identifier for <c>FundCountryAllocation</c>.
/// </summary>
[DebuggerDisplay("FundCountryAllocationId: {Value}")]
public readonly record struct FundCountryAllocationId(Guid Value)
{
    public static FundCountryAllocationId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(value), "Fund country allocation ID must not be empty.");
        return new FundCountryAllocationId(value);
    }

    public static FundCountryAllocationId New() => new(Guid.NewGuid());

    public static implicit operator Guid(FundCountryAllocationId id) => id.Value;
}
