using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for <see cref="Entities.Country"/>.
/// </summary>
/// <remarks>
/// Backed by a <see cref="Guid"/>. Lookup rows are inserted on first encounter and never
/// regenerate their identifier, so allocations can hold a stable FK across re-crawls.
/// </remarks>
[DebuggerDisplay("CountryId: {Value}")]
public readonly record struct CountryId(Guid Value) : IComparable<CountryId>
{
    /// <summary>
    /// Creates a <see cref="CountryId"/> from an existing GUID (rehydrating from the database).
    /// </summary>
    public static CountryId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(value), "Country ID must not be empty.");

        return new CountryId(value);
    }

    /// <summary>
    /// Generates a fresh <see cref="CountryId"/> for a new lookup row.
    /// </summary>
    public static CountryId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public int CompareTo(CountryId other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Implicit conversion to <see cref="Guid"/> for interop with EF Core / value converters.
    /// </summary>
    public static implicit operator Guid(CountryId id) => id.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
