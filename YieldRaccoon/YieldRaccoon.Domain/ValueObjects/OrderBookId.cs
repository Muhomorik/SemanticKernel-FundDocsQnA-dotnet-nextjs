using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for an external orderbook entry.
/// </summary>
/// <remarks>
/// The OrderBookId is an opaque string identifier assigned by the external fund data source.
/// It is used to construct fund detail page URLs and correlate intercepted API responses
/// with the correct fund during browsing sessions.
/// </remarks>
[DebuggerDisplay("OrderBookId: {Value}")]
public readonly record struct OrderBookId(string Value)
{
    /// <summary>
    /// Creates a new <see cref="OrderBookId"/> with validation.
    /// </summary>
    /// <param name="value">The orderbook identifier string.</param>
    /// <returns>A validated <see cref="OrderBookId"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static OrderBookId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("OrderBookId cannot be null or whitespace.", nameof(value));
        }

        return new OrderBookId(value);
    }

    /// <summary>
    /// Parses a string into an <see cref="OrderBookId"/> with validation.
    /// </summary>
    /// <param name="value">The orderbook identifier string to parse.</param>
    /// <returns>A validated <see cref="OrderBookId"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static OrderBookId Parse(string value) => Create(value);
}
