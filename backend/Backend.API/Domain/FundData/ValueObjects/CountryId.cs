using System.Diagnostics;

namespace Backend.API.Domain.FundData.ValueObjects;

/// <summary>
/// Strongly-typed identifier for the <c>Country</c> lookup entity. GUID underneath.
/// </summary>
[DebuggerDisplay("CountryId: {Value}")]
public readonly record struct CountryId(Guid Value)
{
    /// <summary>Creates a <see cref="CountryId"/> from an existing GUID.</summary>
    public static CountryId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(value), "Country ID must not be empty.");
        return new CountryId(value);
    }

    /// <summary>Generates a fresh <see cref="CountryId"/>.</summary>
    public static CountryId New() => new(Guid.NewGuid());

    /// <summary>Implicit conversion to <see cref="Guid"/>.</summary>
    public static implicit operator Guid(CountryId id) => id.Value;
}
