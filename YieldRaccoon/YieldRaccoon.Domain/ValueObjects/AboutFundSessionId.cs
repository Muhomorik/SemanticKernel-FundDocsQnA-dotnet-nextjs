using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Strongly-typed correlation identifier for a single about-fund browsing session.
/// </summary>
/// <remarks>
/// Each about-fund session (navigating through fund detail pages) gets a unique session ID
/// for event correlation and progress tracking.
/// </remarks>
[DebuggerDisplay("AboutFundSessionId: {Value}")]
public readonly record struct AboutFundSessionId(Guid Value)
{
    /// <summary>
    /// Generates a new unique <see cref="AboutFundSessionId"/>.
    /// </summary>
    /// <returns>A new session ID with a generated GUID.</returns>
    public static AboutFundSessionId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Parses a string representation of a GUID into a <see cref="AboutFundSessionId"/>.
    /// </summary>
    /// <param name="value">The GUID string to parse.</param>
    /// <returns>A <see cref="AboutFundSessionId"/> with the parsed GUID.</returns>
    /// <exception cref="FormatException">Thrown if the string is not a valid GUID format.</exception>
    public static AboutFundSessionId Parse(string value) => new(Guid.Parse(value));
}
