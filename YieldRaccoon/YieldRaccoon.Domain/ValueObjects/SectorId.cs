using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for <see cref="Entities.Sector"/>.
/// </summary>
[DebuggerDisplay("SectorId: {Value}")]
public readonly record struct SectorId(Guid Value) : IComparable<SectorId>
{
    /// <summary>
    /// Creates a <see cref="SectorId"/> from an existing GUID (rehydrating from the database).
    /// </summary>
    public static SectorId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(value), "Sector ID must not be empty.");

        return new SectorId(value);
    }

    /// <summary>
    /// Generates a fresh <see cref="SectorId"/> for a new lookup row.
    /// </summary>
    public static SectorId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public int CompareTo(SectorId other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Implicit conversion to <see cref="Guid"/> for interop with EF Core / value converters.
    /// </summary>
    public static implicit operator Guid(SectorId id) => id.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
