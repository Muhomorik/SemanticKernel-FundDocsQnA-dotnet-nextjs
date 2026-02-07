using Microsoft.Web.WebView2.Wpf;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service interface for intercepting WebView2 network responses in the AboutFund browser.
/// Captures ALL network requests for debugging and exploration purposes.
/// </summary>
public interface IAboutFundResponseInterceptor : IDisposable
{
    /// <summary>
    /// Initializes the interceptor and starts monitoring network responses.
    /// Must be called after WebView2 CoreWebView2 is initialized.
    /// </summary>
    /// <param name="webView">The WebView2 control to monitor.</param>
    void Initialize(WebView2 webView);

    /// <summary>
    /// Event raised when a network request/response is intercepted.
    /// </summary>
    event EventHandler<AboutFundInterceptedRequest>? RequestIntercepted;
}
