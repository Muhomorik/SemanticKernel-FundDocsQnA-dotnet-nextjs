using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Application.Configuration;

/// <summary>
/// Defines a URL pattern that maps an intercepted HTTP response to a named
/// data slot on <see cref="AboutFundPageData"/>.
/// </summary>
/// <param name="UrlFragment">
/// A substring to match against the intercepted request URL
/// (e.g., <c>"chart/timeperiods/"</c>). Matching is case-insensitive.
/// </param>
/// <param name="SlotName">
/// The <see cref="AboutFundPageData"/> property name this pattern targets
/// (e.g., <c>nameof(AboutFundPageData.ChartTimePeriods)</c>).
/// </param>
public record EndpointPattern(string UrlFragment, string SlotName);

/// <summary>
/// Configuration for <c>AboutFundResponseParser</c> — maps URL patterns to
/// data collection slots. Registered at the composition root, not exposed
/// in user-facing configuration files.
/// </summary>
/// <param name="Patterns">
/// Ordered list of endpoint patterns. First match wins.
/// </param>
public record ResponseParserOptions(IReadOnlyList<EndpointPattern> Patterns);
