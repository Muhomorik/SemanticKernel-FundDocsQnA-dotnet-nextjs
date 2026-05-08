using System.Diagnostics;

namespace Backend.API.Domain.FundData.ValueObjects;

/// <summary>
/// Strongly-typed identifier for the <c>Sector</c> lookup entity. GUID underneath.
/// </summary>
[DebuggerDisplay("SectorId: {Value}")]
public readonly record struct SectorId(Guid Value)
{
    /// <summary>Creates a <see cref="SectorId"/> from an existing GUID.</summary>
    public static SectorId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(value), "Sector ID must not be empty.");
        return new SectorId(value);
    }

    /// <summary>Generates a fresh <see cref="SectorId"/>.</summary>
    public static SectorId New() => new(Guid.NewGuid());

    /// <summary>Implicit conversion to <see cref="Guid"/>.</summary>
    public static implicit operator Guid(SectorId id) => id.Value;
}
