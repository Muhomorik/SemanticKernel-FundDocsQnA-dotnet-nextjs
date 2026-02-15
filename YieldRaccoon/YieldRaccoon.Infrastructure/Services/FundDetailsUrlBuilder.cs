using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Builds fund detail page URLs by substituting an OrderBookId into a configurable template.
/// </summary>
/// <remarks>
/// The URL template (e.g., <c>https://provider.com/fund/{0}</c>) is an infrastructure
/// concern injected from configuration. This service validates the result is a well-formed URI.
/// </remarks>
public class FundDetailsUrlBuilder : IFundDetailsUrlBuilder
{
    private readonly string _urlTemplate;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundDetailsUrlBuilder"/> class.
    /// </summary>
    /// <param name="urlTemplate">
    /// URL template with <c>{0}</c> placeholder for the OrderBookId.
    /// </param>
    public FundDetailsUrlBuilder(string urlTemplate)
    {
        if (string.IsNullOrWhiteSpace(urlTemplate))
            throw new ArgumentException("URL template cannot be null or whitespace.", nameof(urlTemplate));

        _urlTemplate = urlTemplate;
    }

    /// <inheritdoc />
    public Uri BuildUrl(string orderBookId)
    {
        if (string.IsNullOrWhiteSpace(orderBookId))
            throw new ArgumentException("OrderBookId cannot be null or whitespace.", nameof(orderBookId));

        var url = _urlTemplate.Replace("{0}", orderBookId, StringComparison.OrdinalIgnoreCase);
        return new Uri(url, UriKind.Absolute);
    }
}
