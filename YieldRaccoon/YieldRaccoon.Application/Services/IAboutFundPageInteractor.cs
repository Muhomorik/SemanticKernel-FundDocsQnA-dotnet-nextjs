namespace YieldRaccoon.Application.Services;

/// <summary>
/// Abstracts browser page interactions for the fund detail page data acquisition workflow.
/// </summary>
/// <remarks>
/// <para>
/// Owned by the Application layer; implemented in the Presentation layer (WebView2).
/// The <see cref="IAboutFundOrchestrator"/> uses this to trigger post-navigation
/// interactions (e.g., clicking tabs to reveal additional fund data).
/// </para>
/// <para>
/// Implementations must handle the case where the target element does not exist on the page.
/// </para>
/// </remarks>
public interface IAboutFundPageInteractor
{
    /// <summary>
    /// Performs post-navigation interactions on a fund detail page:
    /// dumps page text for diagnostics, finds the "Utvecklingen i SEK" checkbox, and checks it.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the checkbox was found and clicked;
    /// <see langword="false"/> if the element was not found on the page.
    /// </returns>
    Task<bool> ActivateSekViewAsync();
}
