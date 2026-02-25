using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Builds fund detail page URLs by substituting an OrderBookId into a configurable template.
/// </summary>
/// <remarks>
/// The URL template (e.g., <c>https://provider.com/fund/{0}</c>) is provided via
/// <see cref="FundDetailsUrlBuilderOptions"/> from the Application layer.
/// This service validates the result is a well-formed URI.
/// </remarks>
public class FundDetailsUrlBuilder : IFundDetailsUrlBuilder
{
    private readonly string _urlTemplate;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundDetailsUrlBuilder"/> class.
    /// </summary>
    /// <param name="options">Configuration containing the URL template.</param>
    public FundDetailsUrlBuilder(FundDetailsUrlBuilderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.UrlTemplate))
            throw new ArgumentException("URL template cannot be null or whitespace.", nameof(options));

        _urlTemplate = options.UrlTemplate;
    }

    /// <inheritdoc />
    public Uri BuildUrl(OrderBookId orderBookId)
    {
        var url = _urlTemplate.Replace("{0}", orderBookId.Value, StringComparison.OrdinalIgnoreCase);
        return new Uri(url, UriKind.Absolute);
    }

    /// <inheritdoc />
    public bool TryParseOrderBookId(Uri url, out OrderBookId orderBookId)
    {
        orderBookId = default;

        var parts = _urlTemplate.Split(["{0}"], 2, StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        var urlString = url.AbsoluteUri;
        var prefix = parts[0];
        var suffix = parts[1];

        if (!urlString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var afterPrefix = urlString[prefix.Length..];

        int endIndex;
        if (suffix.Length > 0)
        {
            endIndex = afterPrefix.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
            if (endIndex <= 0)
                return false;
        }
        else
        {
            // No suffix — take everything up to first '?' or '#' or end
            endIndex = afterPrefix.IndexOfAny(['?', '#']);
            if (endIndex < 0) endIndex = afterPrefix.Length;
            if (endIndex == 0) return false;
        }

        var value = afterPrefix[..endIndex];
        if (string.IsNullOrWhiteSpace(value))
            return false;

        orderBookId = OrderBookId.Create(value);
        return true;
    }
}
