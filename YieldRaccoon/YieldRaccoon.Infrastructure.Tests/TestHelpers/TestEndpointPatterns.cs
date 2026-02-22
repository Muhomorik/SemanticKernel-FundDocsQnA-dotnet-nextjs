using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Factory for <see cref="ResponseParserOptions"/> with deterministic URL patterns for tests.
/// </summary>
public static class TestEndpointPatterns
{
    /// <summary>
    /// URL fragment → slot mapping used by <see cref="CreateDefault"/>.
    /// Tests can use these fragments to build matching request URLs.
    /// </summary>
    public static IReadOnlyDictionary<AboutFundDataSlot, string> SlotFragments { get; } =
        new Dictionary<AboutFundDataSlot, string>
        {
            [AboutFundDataSlot.Chart1Month] = "period=1m",
            [AboutFundDataSlot.Chart3Months] = "period=3m",
            [AboutFundDataSlot.ChartYearToDate] = "period=ytd",
            [AboutFundDataSlot.Chart1Year] = "period=1y",
            [AboutFundDataSlot.Chart3Years] = "period=3y",
            [AboutFundDataSlot.Chart5Years] = "period=5y",
            [AboutFundDataSlot.ChartMax] = "period=max",
        };

    /// <summary>
    /// Creates patterns where each slot matches <c>/api/chart</c> + a unique period fragment.
    /// </summary>
    public static ResponseParserOptions CreateDefault() =>
        new(SlotFragments.Select(kv =>
            new EndpointPattern(["/api/chart", kv.Value], kv.Key)).ToList());
}
