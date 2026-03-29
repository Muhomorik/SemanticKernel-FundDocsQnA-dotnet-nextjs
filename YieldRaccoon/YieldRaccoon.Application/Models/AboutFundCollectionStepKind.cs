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

    /// <summary>
    /// Parses persisted step name strings into an enabled step set.
    /// Returns <see cref="Defaults"/> when <paramref name="names"/> is null or empty.
    /// Unknown names are silently ignored for forward compatibility.
    /// </summary>
    public static IReadOnlySet<AboutFundCollectionStepKind> FromNames(IEnumerable<string>? names)
    {
        if (names == null)
            return Defaults;

        var parsed = new HashSet<AboutFundCollectionStepKind>();

        foreach (var name in names)
        {
            if (Enum.TryParse<AboutFundCollectionStepKind>(name, out var step)
                && step != AboutFundCollectionStepKind.ActivateSekView)
            {
                parsed.Add(step);
            }
        }

        return parsed.Count > 0 ? parsed : (IReadOnlySet<AboutFundCollectionStepKind>)Defaults;
    }

    /// <summary>
    /// Converts enabled steps to name strings for persistence.
    /// Returns null when the enabled set matches <see cref="Defaults"/> (avoids storing redundant data).
    /// </summary>
    public static List<string>? ToNames(IEnumerable<AboutFundCollectionStepKind> enabledSteps)
    {
        var set = new HashSet<AboutFundCollectionStepKind>(
            enabledSteps.Where(s => s != AboutFundCollectionStepKind.ActivateSekView));

        if (set.SetEquals(Defaults))
            return null;

        // Preserve canonical order
        return Configurable
            .Where(set.Contains)
            .Select(s => s.ToString())
            .ToList();
    }
}