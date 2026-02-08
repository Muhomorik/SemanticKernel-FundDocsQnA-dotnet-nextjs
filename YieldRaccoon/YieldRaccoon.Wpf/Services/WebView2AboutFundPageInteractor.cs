using Microsoft.Web.WebView2.Wpf;
using NLog;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// WebView2 implementation of <see cref="IAboutFundPageInteractor"/> that executes
/// JavaScript on the loaded page to find and click elements.
/// </summary>
/// <remarks>
/// <para>
/// Follows the same initialization pattern as <see cref="AboutFundResponseInterceptor"/>:
/// call <see cref="Initialize"/> after <c>CoreWebView2InitializationCompleted</c>.
/// </para>
/// <para>
/// Must be called from the UI thread (WebView2 is STA-bound).
/// </para>
/// </remarks>
public class WebView2AboutFundPageInteractor : IAboutFundPageInteractor, IDisposable
{
    private const string SettingsButtonText = "Inställningar";
    private const string SekCheckboxText = "Utvecklingen i SEK";
    private const int PanelOpenDelayMs = 10_000;

    private readonly ILogger _logger;
    private WebView2? _webView;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebView2AboutFundPageInteractor"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public WebView2AboutFundPageInteractor(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Binds this interactor to a WebView2 control.
    /// Must be called after <c>CoreWebView2</c> is initialized.
    /// </summary>
    /// <param name="webView">The WebView2 control to interact with.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>CoreWebView2</c> is not yet initialized.
    /// </exception>
    public void Initialize(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));

        if (_webView.CoreWebView2 == null)
            throw new InvalidOperationException(
                "WebView2 CoreWebView2 must be initialized before calling Initialize()");

        _logger.Info("WebView2AboutFundPageInteractor initialized");
    }

    /// <inheritdoc />
    public async Task<bool> ActivateSekViewAsync()
    {
        if (_webView?.CoreWebView2 == null)
        {
            _logger.Warn("ActivateSekViewAsync called but WebView2 is not initialized");
            return false;
        }

        // 1. Dump page text for diagnostics
        //await DumpPageTextAsync();

        // 2. Check if "Inställningar" button exists, then click it to open the settings side panel
        var buttonExists = await ElementExistsByTextAsync(SettingsButtonText);
        if (!buttonExists)
        {
            _logger.Debug("'{0}' button not found on page", SettingsButtonText);
            return false;
        }

        var panelOpened = await ClickAncestorByTextAsync(SettingsButtonText, "button");
        if (!panelOpened)
        {
            _logger.Debug("'{0}' button not found — skipping SEK checkbox", SettingsButtonText);
            return false;
        }

        // 3. Wait for the side panel animation to complete
        await Task.Delay(PanelOpenDelayMs);

        // 4. Check if the checkbox element exists in the panel
        var exists = await ElementExistsByTextAsync(SekCheckboxText);
        if (!exists)
        {
            _logger.Debug("'{0}' checkbox not found in settings panel — skipping click", SekCheckboxText);
            return false;
        }

        // 5. Wait for the side panel animation to complete
        await Task.Delay(PanelOpenDelayMs);

        // 6. Click the checkbox label to toggle it
        var clicked = await ClickAncestorByTextAsync(SekCheckboxText, "label");
        _logger.Info("ActivateSekViewAsync: {0}", clicked ? "checkbox clicked" : "click failed");
        return clicked;
    }

    /// <summary>
    /// Checks whether a leaf element containing <paramref name="text"/> exists on the page.
    /// </summary>
    private async Task<bool> ElementExistsByTextAsync(string text)
    {
        var escapedText = text.Replace("'", "\\'");

        var script = $@"(function() {{
  var elements = document.querySelectorAll('*');
  for (var i = 0; i < elements.length; i++) {{
    var el = elements[i];
    if (el.children.length === 0 && el.textContent.indexOf('{escapedText}') !== -1) {{
      return 'yes';
    }}
  }}
  return 'no';
}})()";

        var result = await _webView!.CoreWebView2.ExecuteScriptAsync(script);
        var found = result == "\"yes\"";

        _logger.Debug("ElementExistsByTextAsync('{0}'): {1} (raw: {2})", text, found ? "found" : "not_found", result);
        return found;
    }

    /// <summary>
    /// Finds a leaf element containing <paramref name="text"/> and clicks its closest
    /// <paramref name="ancestorTag"/> ancestor (e.g., <c>"button"</c> or <c>"label"</c>).
    /// Falls back to clicking the element itself if no matching ancestor is found.
    /// </summary>
    private async Task<bool> ClickAncestorByTextAsync(string text, string ancestorTag)
    {
        var escapedText = text.Replace("'", "\\'");
        var escapedTag = ancestorTag.Replace("'", "\\'");

        var script = $@"(function() {{
  var elements = document.querySelectorAll('*');
  for (var i = 0; i < elements.length; i++) {{
    var el = elements[i];
    if (el.children.length === 0 && el.textContent.indexOf('{escapedText}') !== -1) {{
      var ancestor = el.closest('{escapedTag}');
      if (ancestor) {{
        ancestor.click();
        return 'clicked_{escapedTag}';
      }}
      el.click();
      return 'clicked_element';
    }}
  }}
  return 'no';
}})()";

        var result = await _webView!.CoreWebView2.ExecuteScriptAsync(script);
        var clicked = result != "\"no\"";

        _logger.Debug("ClickAncestorByTextAsync('{0}', '{1}'): {2} (raw: {3})",
            text, ancestorTag, clicked ? "clicked" : "not_found", result);
        return clicked;
    }

    /// <summary>
    /// Dumps <c>document.body.innerText</c> to NLog for diagnostics.
    /// </summary>
    private async Task DumpPageTextAsync()
    {
        var script = "(function() { return document.body.innerText; })()";
        var result = await _webView!.CoreWebView2.ExecuteScriptAsync(script);

        // Result is JSON-encoded string — strip surrounding quotes and unescape
        if (result.Length >= 2 && result.StartsWith('"') && result.EndsWith('"'))
            result = result[1..^1]
                .Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");

        _logger.Info("=== PAGE TEXT DUMP ({0} chars) ===\n{1}\n=== END DUMP ===", result.Length, result);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _logger.Debug("WebView2AboutFundPageInteractor disposing");
        _webView = null;
        _disposed = true;
    }
}
