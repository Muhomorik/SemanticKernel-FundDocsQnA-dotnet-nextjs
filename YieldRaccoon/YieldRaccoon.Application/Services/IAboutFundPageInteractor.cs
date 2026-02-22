using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Abstracts browser page interactions for the fund detail page data acquisition workflow.
/// </summary>
/// <remarks>
/// <para>
/// Owned by the Application layer; implemented in the Presentation layer (WebView2).
/// </para>
/// <para>
/// Implementations must handle errors internally (including browser exceptions) and
/// return <see langword="false"/> on failure — they must never throw.
/// </para>
/// </remarks>
public interface IAboutFundPageInteractor
{
    
    /// <summary>
    /// Returns the minimum time an interaction takes for the given step kind.
    /// Used by the scheduler to set a meaningful lower bound on the delay before each step.
    /// </summary>
    /// <param name="stepKind">The collection step to query.</param>
    TimeSpan GetMinimumDelay(AboutFundCollectionStepKind stepKind);
    
    /// <summary>
    /// Opens the settings side panel and checks the "Utvecklingen i SEK" checkbox
    /// to switch the chart to SEK-denominated view.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the checkbox was found and clicked;
    /// <see langword="false"/> if the element was not found or an error occurred.
    /// </returns>
    Task<bool> ActivateSekViewAsync();

    /// <summary>
    /// Selects the "1 month" time period on the fund detail chart.
    /// </summary>
    /// <returns><see langword="true"/> if the button was found and clicked; <see langword="false"/> otherwise.</returns>
    Task<bool> SelectPeriod1MonthAsync();

    /// <summary>
    /// Selects the "3 months" time period on the fund detail chart.
    /// </summary>
    /// <returns><see langword="true"/> if the button was found and clicked; <see langword="false"/> otherwise.</returns>
    Task<bool> SelectPeriod3MonthsAsync();

    /// <summary>
    /// Selects the "this year" (YTD) time period on the fund detail chart.
    /// </summary>
    /// <returns><see langword="true"/> if the button was found and clicked; <see langword="false"/> otherwise.</returns>
    Task<bool> SelectPeriodYearToDateAsync();

    /// <summary>
    /// Selects the "1 year" time period on the fund detail chart.
    /// </summary>
    /// <returns><see langword="true"/> if the button was found and clicked; <see langword="false"/> otherwise.</returns>
    Task<bool> SelectPeriod1YearAsync();

    /// <summary>
    /// Selects the "3 years" time period on the fund detail chart.
    /// </summary>
    /// <returns><see langword="true"/> if the button was found and clicked; <see langword="false"/> otherwise.</returns>
    Task<bool> SelectPeriod3YearsAsync();

    /// <summary>
    /// Selects the "5 years" time period on the fund detail chart.
    /// </summary>
    /// <returns><see langword="true"/> if the button was found and clicked; <see langword="false"/> otherwise.</returns>
    Task<bool> SelectPeriod5YearsAsync();

    /// <summary>
    /// Selects the "max" time period on the fund detail chart.
    /// </summary>
    /// <returns><see langword="true"/> if the button was found and clicked; <see langword="false"/> otherwise.</returns>
    Task<bool> SelectPeriodMaxAsync();
}
