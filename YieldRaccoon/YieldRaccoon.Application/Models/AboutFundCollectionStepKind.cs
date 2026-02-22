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
}