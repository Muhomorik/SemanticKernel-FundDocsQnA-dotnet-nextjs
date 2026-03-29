using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Strongly-typed correlation identifier for a single crawl session.
/// </summary>
/// <remarks>
/// Each crawl session (navigating 4 tabs for one fund) gets a unique session ID
/// for event correlation and progress tracking.
/// </remarks>
[DebuggerDisplay("SessionId: {Value}")]
public readonly record struct FundListSessionId(Guid Value)
{
    /// <summary>
    /// Generates a new unique <see cref="FundListSessionId"/>.
    /// </summary>
    /// <returns>A new session ID with a generated GUID.</returns>
    public static FundListSessionId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Parses a string representation of a GUID into a <see cref="FundListSessionId"/>.
    /// </summary>
    /// <param name="value">The GUID string to parse.</param>
    /// <returns>A <see cref="FundListSessionId"/> with the parsed GUID.</returns>
    /// <exception cref="FormatException">Thrown if the string is not a valid GUID format.</exception>
    public static FundListSessionId Parse(string value) => new(Guid.Parse(value));
}
