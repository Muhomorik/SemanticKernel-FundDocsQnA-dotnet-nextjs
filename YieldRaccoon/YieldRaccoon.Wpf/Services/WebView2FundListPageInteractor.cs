using Microsoft.Web.WebView2.Wpf;
using NLog;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// WebView2 implementation of <see cref="IFundListPageInteractor"/> that executes
/// JavaScript on the fund list page for pagination, scrolling, and reloading.
/// </summary>
/// <remarks>
/// <para>
/// Follows the same initialization pattern as <see cref="WebView2AboutFundPageInteractor"/>:
/// call <see cref="Initialize"/> after <c>CoreWebView2InitializationCompleted</c>.
/// </para>
/// <para>
/// Must be called from the UI thread (WebView2 is STA-bound).
/// </para>
/// </remarks>
public class WebView2FundListPageInteractor : IFundListPageInteractor
{
    private readonly ILogger _logger;
    private WebView2? _webView;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebView2FundListPageInteractor"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public WebView2FundListPageInteractor(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Initialize(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));

        if (_webView.CoreWebView2 == null)
            throw new InvalidOperationException(
                "WebView2 CoreWebView2 must be initialized before calling Initialize()");

        _logger.Info("WebView2FundListPageInteractor initialized");
    }

    /// <inheritdoc />
    public async Task<bool> ClickLoadMoreButtonAsync()
    {
        if (_webView?.CoreWebView2 == null)
        {
            _logger.Warn("ClickLoadMoreButtonAsync called but WebView2 is not initialized");
            return false;
        }

        try
        {
            // Wait a bit for the page to settle after the previous load
            await Task.Delay(500);

            // JavaScript to find and click the "Visa fler" button
            // The button typically has Swedish text "Visa fler"
            var clickButtonScript = @"
                (function() {
                    // Try multiple selectors to find the 'Visa fler' button

                    // Method 1: Find button by text content
                    const buttons = Array.from(document.querySelectorAll('button'));
                    const visaFlerButton = buttons.find(btn =>
                        btn.textContent && btn.textContent.toLowerCase().includes('visa fler')
                    );

                    if (visaFlerButton) {
                        visaFlerButton.click();
                        return 'Clicked Visa fler button (by text)';
                    }

                    // Method 2: Common class names for load more buttons
                    const loadMoreButton = document.querySelector('.load-more, .show-more, [data-testid*=""load""], [data-testid*=""more""]');
                    if (loadMoreButton) {
                        loadMoreButton.click();
                        return 'Clicked load more button (by class)';
                    }

                    // Method 3: Find by aria-label
                    const ariaButton = document.querySelector('[aria-label*=""visa""][aria-label*=""fler""], [aria-label*=""load""][aria-label*=""more""]');
                    if (ariaButton) {
                        ariaButton.click();
                        return 'Clicked button (by aria-label)';
                    }

                    return 'Button not found';
                })();
            ";

            var result = await _webView.CoreWebView2.ExecuteScriptAsync(clickButtonScript);
            _logger.Info("Click button script result: {0}", result);

            var buttonFound = !result.Contains("not found", StringComparison.OrdinalIgnoreCase);
            if (!buttonFound)
                _logger.Warn("'Visa fler' button not found on page");

            return buttonFound;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while trying to click 'Visa fler' button");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task ScrollToEndAsync()
    {
        if (_webView?.CoreWebView2 == null)
        {
            _logger.Warn("Cannot scroll - WebView2 not initialized");
            return;
        }

        try
        {
            var scrollScript = @"
                (function() {
                    window.scrollTo({
                        top: document.body.scrollHeight,
                        behavior: 'smooth'
                    });
                    return 'scrolled';
                })();
            ";

            await _webView.CoreWebView2.ExecuteScriptAsync(scrollScript);
            _logger.Debug("Browser smooth scroll to bottom executed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to execute browser scroll");
        }
    }

    /// <inheritdoc />
    public void Reload()
    {
        if (_webView?.CoreWebView2 != null)
        {
            _logger.Debug("Browser reload requested");
            _webView.CoreWebView2.Reload();
        }
        else
        {
            _logger.Warn("Cannot reload: CoreWebView2 is not initialized");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _logger.Debug("WebView2FundListPageInteractor disposing");
        _webView = null;
        _disposed = true;
    }
}
