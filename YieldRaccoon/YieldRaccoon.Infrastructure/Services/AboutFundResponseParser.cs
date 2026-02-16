using NLog;
using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Maps intercepted HTTP responses to <see cref="IAboutFundPageDataCollector"/> slot updates
/// by matching URL patterns specific to the about-fund detail page.
/// </summary>
/// <remarks>
/// <para>
/// This class encapsulates the knowledge of which API endpoints correspond to which
/// data slots in <see cref="AboutFundPageData"/>. It is intentionally in the Infrastructure
/// layer because URL patterns are infrastructure concerns that the Domain and Application
/// layers should not know about.
/// </para>
/// <para>
/// When new data endpoints are discovered on the fund detail page, add a new
/// <see cref="EndpointPattern"/> to the <see cref="ResponseParserOptions"/> registered
/// at the composition root.
/// </para>
/// </remarks>
public class AboutFundResponseParser
{
    private readonly ILogger _logger;
    private readonly IAboutFundPageDataCollector _collector;
    private readonly IReadOnlyList<EndpointPattern> _patterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundResponseParser"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="collector">The collector to route parsed responses to.</param>
    /// <param name="options">Endpoint patterns configuration.</param>
    public AboutFundResponseParser(ILogger logger, IAboutFundPageDataCollector collector, ResponseParserOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        ArgumentNullException.ThrowIfNull(options);
        _patterns = options.Patterns;
    }

    /// <summary>
    /// Attempts to match an intercepted request's URL to a known slot and routes
    /// the response data to the collector.
    /// </summary>
    /// <param name="request">The intercepted HTTP request/response.</param>
    /// <returns><c>true</c> if the URL matched a known pattern and was routed; <c>false</c> otherwise.</returns>
    public bool TryRoute(AboutFundInterceptedRequest request)
    {
        foreach (var endpoint in _patterns)
        {
            if (!request.Url.Contains(endpoint.UrlFragment, StringComparison.OrdinalIgnoreCase))
                continue;

            if (request.StatusCode is < 200 or >= 300)
            {
                _logger.Warn("Matched {0} but status {1} — marking slot failed", endpoint.UrlFragment, request.StatusCode);
                _collector.FailSlot(endpoint.SlotName, $"HTTP {request.StatusCode}: {request.StatusText}");
                return true;
            }

            if (string.IsNullOrEmpty(request.ResponsePreview))
            {
                _logger.Warn("Matched {0} but response body is empty — marking slot failed", endpoint.UrlFragment);
                _collector.FailSlot(endpoint.SlotName, "Empty response body");
                return true;
            }

            _logger.Debug("Matched {0} → {1} ({2} chars)", endpoint.UrlFragment, endpoint.SlotName, request.ResponsePreview.Length);

            // Route to the appropriate slot
            switch (endpoint.SlotName)
            {
                case nameof(AboutFundPageData.ChartTimePeriods):
                    _collector.ReceiveChartTimePeriods(request.ResponsePreview);
                    break;
                case nameof(AboutFundPageData.SekPerformance):
                    _collector.ReceiveSekPerformance(request.ResponsePreview);
                    break;
            }

            return true;
        }

        return false;
    }
}
