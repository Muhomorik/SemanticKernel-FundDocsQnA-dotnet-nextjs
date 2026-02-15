namespace YieldRaccoon.Application.Services;

/// <summary>
/// Builds strongly-typed <see cref="Uri"/> instances for fund detail page navigation.
/// </summary>
/// <remarks>
/// Encapsulates the knowledge of how fund detail URLs are constructed from identifiers.
/// The orchestrator uses this to emit <see cref="Uri"/> navigation intents rather than
/// raw strings, pushing URL validation to the point of creation.
/// </remarks>
public interface IFundDetailsUrlBuilder
{
    /// <summary>
    /// Builds the fund details page URL for the given OrderBookId.
    /// </summary>
    /// <param name="orderBookId">The fund's OrderBookId used in the external URL.</param>
    /// <returns>A validated <see cref="Uri"/> for the fund detail page.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="orderBookId"/> is null or whitespace.
    /// </exception>
    Uri BuildUrl(string orderBookId);
}
