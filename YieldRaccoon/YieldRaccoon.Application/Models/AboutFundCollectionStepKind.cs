namespace YieldRaccoon.Application.Models;

/// <summary>
/// Identifies each scheduled interaction step during a fund page visit.
/// </summary>
public enum AboutFundCollectionStepKind
{
    ActivateSekView,
    Select1Month,
    Select3Months,
    SelectYearToDate,
    Select1Year,
    Select3Years,
    Select5Years,
    SelectMax
}

/// <summary>
/// Provides the ordered list of all collection step kinds for schedule pre-calculation.
/// </summary>
public static class AboutFundCollectionStepKinds
{
    /// <summary>
    /// All step kinds in execution order.
    /// </summary>
    public static IReadOnlyList<AboutFundCollectionStepKind> All { get; } =
    [
        AboutFundCollectionStepKind.ActivateSekView,
        AboutFundCollectionStepKind.Select1Month,
        AboutFundCollectionStepKind.Select3Months,
        AboutFundCollectionStepKind.SelectYearToDate,
        AboutFundCollectionStepKind.Select1Year,
        AboutFundCollectionStepKind.Select3Years,
        AboutFundCollectionStepKind.Select5Years,
        AboutFundCollectionStepKind.SelectMax
    ];

    /// <summary>
    /// The 7 configurable step kinds (all except <see cref="AboutFundCollectionStepKind.ActivateSekView"/>
    /// which is always required).
    /// </summary>
    public static IReadOnlyList<AboutFundCollectionStepKind> Configurable { get; } =
    [
        AboutFundCollectionStepKind.Select1Month,
        AboutFundCollectionStepKind.Select3Months,
        AboutFundCollectionStepKind.SelectYearToDate,
        AboutFundCollectionStepKind.Select1Year,
        AboutFundCollectionStepKind.Select3Years,
        AboutFundCollectionStepKind.Select5Years,
        AboutFundCollectionStepKind.SelectMax
    ];

    /// <summary>
    /// Default-enabled step kinds for new sessions.
    /// </summary>
    public static IReadOnlySet<AboutFundCollectionStepKind> Defaults { get; } = new HashSet<AboutFundCollectionStepKind>
    {
        AboutFundCollectionStepKind.Select1Month,
        AboutFundCollectionStepKind.Select3Months,
        AboutFundCollectionStepKind.SelectYearToDate,
        AboutFundCollectionStepKind.Select1Year,
        AboutFundCollectionStepKind.Select3Years,
        AboutFundCollectionStepKind.Select5Years,
        AboutFundCollectionStepKind.SelectMax
    };

    /// <summary>
    /// Returns the ordered step list for a given set of enabled steps.
    /// Always prepends <see cref="AboutFundCollectionStepKind.ActivateSekView"/>
    /// and preserves the canonical order from <see cref="All"/>.
    /// </summary>
    public static IReadOnlyList<AboutFundCollectionStepKind> ForSteps(
        IEnumerable<AboutFundCollectionStepKind> enabledSteps)
    {
        var enabled = new HashSet<AboutFundCollectionStepKind>(enabledSteps);
        var result = new List<AboutFundCollectionStepKind> { AboutFundCollectionStepKind.ActivateSekView };

        foreach (var step in Configurable)
        {
            if (enabled.Contains(step))
                result.Add(step);
        }

        return result;
    }
}