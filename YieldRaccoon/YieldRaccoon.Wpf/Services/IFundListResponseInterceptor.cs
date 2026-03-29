using Microsoft.Web.WebView2.Wpf;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service interface for intercepting WebView2 network responses to capture fund list data.
/// </summary>
/// <remarks>
/// Follows the same initialization pattern as <see cref="IAboutFundResponseInterceptor"/>:
/// call <see cref="Initialize"/> after <c>CoreWebView2InitializationCompleted</c>.
/// </remarks>
public interface IFundListResponseInterceptor : IDisposable
{
    /// <summary>
    /// Initializes the interceptor and starts monitoring network responses.
    /// Must be called after WebView2 CoreWebView2 is initialized.
    /// </summary>
    /// <param name="webView">The WebView2 control to monitor.</param>
    void Initialize(WebView2 webView);

    /// <summary>
    /// Event raised when fund list data is intercepted from a network response.
    /// </summary>
    event EventHandler<FundListDataInterceptedEventArgs>? FundDataIntercepted;
}
