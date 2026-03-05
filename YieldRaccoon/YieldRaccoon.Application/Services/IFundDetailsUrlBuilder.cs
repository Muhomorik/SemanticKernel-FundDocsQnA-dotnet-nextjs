using YieldRaccoon.Domain.ValueObjects;

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
    Uri BuildUrl(OrderBookId orderBookId);

    /// <summary>
    /// Attempts to extract the <see cref="OrderBookId"/> from a fund detail page URL.
    /// </summary>
    /// <remarks>
    /// Reverse of <see cref="BuildUrl"/>: splits the URL template at the <c>{0}</c> placeholder
    /// and extracts the segment between the prefix and suffix.
    /// </remarks>
    /// <param name="url">The URL to parse.</param>
    /// <param name="orderBookId">When successful, the extracted OrderBookId; otherwise, <c>default</c>.</param>
    /// <returns><c>true</c> if the URL matches the template and an OrderBookId was extracted; <c>false</c> otherwise.</returns>
    bool TryParseOrderBookId(Uri url, out OrderBookId orderBookId);
}
