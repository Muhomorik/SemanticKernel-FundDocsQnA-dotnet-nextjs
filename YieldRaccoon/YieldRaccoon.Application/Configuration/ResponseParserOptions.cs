using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Application.Configuration;

/// <summary>
/// Defines a URL pattern that maps an intercepted HTTP response to a named
/// data slot on <see cref="AboutFundPageData"/>.
/// </summary>
/// <param name="UrlFragments">
/// Substrings that must all be present in the intercepted request URL
/// for this pattern to match. Matching is case-insensitive.
/// </param>
/// <param name="Slot">
/// The <see cref="AboutFundPageData"/> slot this pattern targets.
/// </param>
public record EndpointPattern(IReadOnlyList<string> UrlFragments, AboutFundDataSlot Slot);

/// <summary>
/// Configuration for response routing — maps URL patterns to
/// data collection slots. Registered at the composition root, not exposed
/// in user-facing configuration files.
/// </summary>
/// <param name="Patterns">
/// Ordered list of endpoint patterns. First match wins.
/// </param>
public record ResponseParserOptions(IReadOnlyList<EndpointPattern> Patterns);
