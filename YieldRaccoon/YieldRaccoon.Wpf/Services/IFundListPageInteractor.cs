using Microsoft.Web.WebView2.Wpf;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Abstracts browser page interactions for the fund list page.
/// Handles pagination button clicks, scrolling, and page reloading.
/// </summary>
/// <remarks>
/// <para>
/// Lives in the Presentation layer because it is only consumed by the MainWindow code-behind.
/// Unlike <see cref="Application.Services.IAboutFundPageInteractor"/>, no Infrastructure service
/// depends on this interface — the <see cref="Application.Services.IFundListOrchestrator"/>
/// uses the Intent Signal Pattern instead.
/// </para>
/// <para>
/// Follows the same initialization pattern as <see cref="IAboutFundResponseInterceptor"/>:
/// call <see cref="Initialize"/> after <c>CoreWebView2InitializationCompleted</c>.
/// </para>
/// </remarks>
public interface IFundListPageInteractor : IDisposable
{
    /// <summary>
    /// Binds this interactor to a WebView2 control.
    /// Must be called after CoreWebView2 is initialized.
    /// </summary>
    /// <param name="webView">The WebView2 control to interact with.</param>
    void Initialize(WebView2 webView);

    /// <summary>
    /// Finds and clicks the "Visa fler" pagination button on the fund list page.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the button was found and clicked;
    /// <see langword="false"/> if the button was not found (no more pages).
    /// </returns>
    Task<bool> ClickLoadMoreButtonAsync();

    /// <summary>
    /// Smooth-scrolls the page to the bottom.
    /// </summary>
    Task ScrollToEndAsync();

    /// <summary>
    /// Reloads the current page.
    /// </summary>
    void Reload();
}
