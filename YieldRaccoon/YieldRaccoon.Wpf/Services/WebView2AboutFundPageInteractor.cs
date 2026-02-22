using Microsoft.Web.WebView2.Wpf;
using NLog;
using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Application.Models;
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
/// <para>
/// <b>IMPORTANT — Click sequence contract:</b> All page interactions MUST follow
/// a three-step sequence: (1) check element exists, (2) wait <see cref="PageInteractorOptions.MinDelayMs"/>,
/// (3) click. Never click without checking existence first. Never skip the delay
/// between check and click. This prevents clicking stale or not-yet-rendered elements
/// and gives the page time to settle between actions.
/// </para>
/// </remarks>
public class WebView2AboutFundPageInteractor : IAboutFundPageInteractor, IDisposable
{
    private const string SettingsButtonAriaLabel = "Grafinställningar";
    private const string SekCheckboxText = "Utvecklingen i SEK";
    private const string CloseButtonAriaLabel = "Stäng";
    private const string TimePeriod1Month = "one_month";
    private const string TimePeriod3Months = "three_months";
    private const string TimePeriodYearToDate = "this_year";
    private const string TimePeriod1Year = "one_year";
    private const string TimePeriod3Years = "three_years";
    private const string TimePeriod5Years = "five_years";
    private const string TimePeriodMax = "infinity";

    private readonly ILogger _logger;
    private readonly PageInteractorOptions _options;
    private bool _disposed;

    private WebView2? _webView;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebView2AboutFundPageInteractor"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="options">Configurable delay timings for page interactions.</param>
    public WebView2AboutFundPageInteractor(ILogger logger, PageInteractorOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
    public TimeSpan GetMinimumDelay(AboutFundCollectionStepKind stepKind)
    {
        return stepKind switch
        {
            AboutFundCollectionStepKind.ActivateSekView => TimeSpan.FromMilliseconds(_options.MinDelayMs + 3 * _options.PanelOpenDelayMs),
            _ => TimeSpan.FromMilliseconds(2 * _options.MinDelayMs)
        };
    }

    /// <inheritdoc />
    public async Task<bool> ActivateSekViewAsync()
    {
        if (_webView?.CoreWebView2 == null)
        {
            _logger.Warn("ActivateSekViewAsync called but WebView2 is not initialized");
            return false;
        }

        try
        {
            // 1. Check if settings button exists, then click it to open the settings side panel
            var buttonExists = await ButtonExistsByAriaLabelAsync(SettingsButtonAriaLabel);
            if (!buttonExists)
            {
                _logger.Debug("Settings button (aria-label='{0}') not found on page", SettingsButtonAriaLabel);
                return false;
            }

            await Task.Delay(_options.MinDelayMs);

            var panelOpened = await ClickButtonByAriaLabelAsync(SettingsButtonAriaLabel);
            if (!panelOpened)
            {
                _logger.Debug("Settings button (aria-label='{0}') click failed", SettingsButtonAriaLabel);
                return false;
            }

            // 2. Wait for the side panel animation to complete
            await Task.Delay(_options.PanelOpenDelayMs);

            // 3. Check if the checkbox element exists in the panel
            var exists = await ElementExistsByTextAsync(SekCheckboxText);
            if (!exists)
            {
                _logger.Debug("'{0}' checkbox not found in settings panel — skipping click", SekCheckboxText);
                return false;
            }

            // 4. Wait for the side panel animation to complete
            await Task.Delay(_options.PanelOpenDelayMs);

            // 5. Check if the checkbox is already checked — skip the click if so
            var alreadyChecked = await IsCheckboxCheckedByTextAsync(SekCheckboxText);
            if (alreadyChecked)
            {
                _logger.Info("ActivateSekViewAsync: checkbox already checked — skipping toggle");
            }
            else
            {
                var clicked = await ClickAncestorByTextAsync(SekCheckboxText, "label");
                _logger.Info("ActivateSekViewAsync: {0}", clicked ? "checkbox clicked" : "click failed");

                if (!clicked) return false;
            }

            // 6. Check if the close button exists in the panel
            var closeExists = await ButtonExistsByAriaLabelAsync(CloseButtonAriaLabel);
            if (!closeExists)
            {
                _logger.Debug("Close button (aria-label='{0}') not found — skipping", CloseButtonAriaLabel);
                return true;
            }

            // 7. Wait for the animation, then click the close button to dismiss the panel
            await Task.Delay(_options.PanelOpenDelayMs);

            var closed = await ClickButtonByAriaLabelAsync(CloseButtonAriaLabel);
            _logger.Info("ActivateSekViewAsync: close panel {0}", closed ? "succeeded" : "failed");

            return true;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "ActivateSekViewAsync failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SelectPeriod1MonthAsync()
    {
        return await SelectTimePeriodAsync(TimePeriod1Month);
    }

    /// <inheritdoc />
    public async Task<bool> SelectPeriod3MonthsAsync()
    {
        return await SelectTimePeriodAsync(TimePeriod3Months);
    }

    /// <inheritdoc />
    public async Task<bool> SelectPeriodYearToDateAsync()
    {
        return await SelectTimePeriodAsync(TimePeriodYearToDate);
    }

    /// <inheritdoc />
    public async Task<bool> SelectPeriod1YearAsync()
    {
        return await SelectTimePeriodAsync(TimePeriod1Year);
    }

    /// <inheritdoc />
    public async Task<bool> SelectPeriod3YearsAsync()
    {
        return await SelectTimePeriodAsync(TimePeriod3Years);
    }

    /// <inheritdoc />
    public async Task<bool> SelectPeriod5YearsAsync()
    {
        return await SelectTimePeriodAsync(TimePeriod5Years);
    }

    /// <inheritdoc />
    public async Task<bool> SelectPeriodMaxAsync()
    {
        return await SelectTimePeriodAsync(TimePeriodMax);
    }

    #region side panel

    /// <summary>
    /// Checks whether a <c>&lt;button data-timeperiod="..."&gt;</c> element exists on the page.
    /// </summary>
    private async Task<bool> TimePeriodButtonExistsAsync(string timePeriod)
    {
        try
        {
            var script = $@"(function() {{
  var btn = document.querySelector('button[data-timeperiod=""{timePeriod}""]');
  return btn ? 'yes' : 'no';
}})()";

            var result = await _webView!.CoreWebView2.ExecuteScriptAsync(script);
            var found = result == "\"yes\"";

            _logger.Debug("TimePeriodButtonExistsAsync('{0}'): {1} (raw: {2})",
                timePeriod, found ? "found" : "not_found", result);
            return found;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "TimePeriodButtonExistsAsync('{0}') failed", timePeriod);
            return false;
        }
    }

    /// <summary>
    /// Finds a leaf element containing <paramref name="text"/> and clicks its closest
    /// <c>button</c> ancestor. Falls back to clicking the element itself.
    /// Waits <see cref="PageInteractorOptions.MinDelayMs"/> after a successful click to let the page react.
    /// Returns <c>false</c> if the element is not found.
    /// </summary>
    private async Task<bool> ClickButtonByTextAsync(string text)
    {
        if (_webView?.CoreWebView2 == null)
        {
            _logger.Warn("ClickButtonByTextAsync('{0}') called but WebView2 is not initialized", text);
            return false;
        }

        try
        {
            var escapedText = text.Replace("'", "\\'");

            var script = $@"(function() {{
  var elements = document.querySelectorAll('*');
  for (var i = 0; i < elements.length; i++) {{
    var el = elements[i];
    if (el.children.length === 0 && el.textContent.trim() === '{escapedText}') {{
      var btn = el.closest('button');
      if (btn) {{
        btn.click();
        return 'clicked_button';
      }}
      el.click();
      return 'clicked_element';
    }}
  }}
  return 'not_found';
}})()";

            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            var clicked = result != "\"not_found\"";

            _logger.Debug("ClickButtonByTextAsync('{0}'): {1} (raw: {2})",
                text, clicked ? "clicked" : "not_found", result);

            if (clicked)
                await Task.Delay(_options.MinDelayMs);

            return clicked;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "ClickButtonByTextAsync('{0}') failed", text);
            return false;
        }
    }

    #endregion

    #region aria-label helpers

    /// <summary>
    /// Checks whether a <c>&lt;button aria-label="..."&gt;</c> element exists on the page.
    /// </summary>
    private async Task<bool> ButtonExistsByAriaLabelAsync(string ariaLabel)
    {
        try
        {
            var escapedLabel = ariaLabel.Replace("'", "\\'");

            var script = $@"(function() {{
  var btn = document.querySelector('button[aria-label=""{escapedLabel}""]');
  return btn ? 'yes' : 'no';
}})()";

            var result = await _webView!.CoreWebView2.ExecuteScriptAsync(script);
            var found = result == "\"yes\"";

            _logger.Debug("ButtonExistsByAriaLabelAsync('{0}'): {1} (raw: {2})",
                ariaLabel, found ? "found" : "not_found", result);
            return found;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "ButtonExistsByAriaLabelAsync('{0}') failed", ariaLabel);
            return false;
        }
    }

    /// <summary>
    /// Finds a <c>&lt;button aria-label="..."&gt;</c> element and clicks it.
    /// </summary>
    private async Task<bool> ClickButtonByAriaLabelAsync(string ariaLabel)
    {
        try
        {
            var escapedLabel = ariaLabel.Replace("'", "\\'");

            var script = $@"(function() {{
  var btn = document.querySelector('button[aria-label=""{escapedLabel}""]');
  if (btn) {{ btn.click(); return 'clicked'; }}
  return 'not_found';
}})()";

            var result = await _webView!.CoreWebView2.ExecuteScriptAsync(script);
            var clicked = result != "\"not_found\"";

            _logger.Debug("ClickButtonByAriaLabelAsync('{0}'): {1} (raw: {2})",
                ariaLabel, clicked ? "clicked" : "not_found", result);
            return clicked;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "ClickButtonByAriaLabelAsync('{0}') failed", ariaLabel);
            return false;
        }
    }

    #endregion

    #region root view

    /// <summary>
    /// Checks whether a leaf element containing <paramref name="text"/> exists on the page.
    /// </summary>
    private async Task<bool> ElementExistsByTextAsync(string text)
    {
        try
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

            _logger.Debug("ElementExistsByTextAsync('{0}'): {1} (raw: {2})", text, found ? "found" : "not_found",
                result);
            return found;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "ElementExistsByTextAsync('{0}') failed", text);
            return false;
        }
    }

    /// <summary>
    /// Finds a leaf element containing <paramref name="text"/>, walks up to the closest
    /// <c>&lt;label&gt;</c>, and checks whether the associated <c>&lt;input&gt;</c> checkbox
    /// is checked. Returns <c>false</c> if the element or checkbox is not found.
    /// </summary>
    private async Task<bool> IsCheckboxCheckedByTextAsync(string text)
    {
        try
        {
            var escapedText = text.Replace("'", "\\'");

            var script = $@"(function() {{
  var elements = document.querySelectorAll('*');
  for (var i = 0; i < elements.length; i++) {{
    var el = elements[i];
    if (el.children.length === 0 && el.textContent.indexOf('{escapedText}') !== -1) {{
      var label = el.closest('label');
      if (label) {{
        var input = label.querySelector('input[type=""checkbox""]');
        if (!input && label.htmlFor) {{
          input = document.getElementById(label.htmlFor);
        }}
        if (input) return input.checked ? 'checked' : 'unchecked';
      }}
      return 'no_checkbox';
    }}
  }}
  return 'not_found';
}})()";

            var result = await _webView!.CoreWebView2.ExecuteScriptAsync(script);
            var isChecked = result == "\"checked\"";

            _logger.Debug("IsCheckboxCheckedByTextAsync('{0}'): {1} (raw: {2})",
                text, isChecked ? "checked" : "not_checked", result);
            return isChecked;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "IsCheckboxCheckedByTextAsync('{0}') failed", text);
            return false;
        }
    }

    /// <summary>
    /// Finds a leaf element containing <paramref name="text"/> and clicks its closest
    /// <paramref name="ancestorTag"/> ancestor (e.g., <c>"button"</c> or <c>"label"</c>).
    /// Falls back to clicking the element itself if no matching ancestor is found.
    /// </summary>
    private async Task<bool> ClickAncestorByTextAsync(string text, string ancestorTag)
    {
        try
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
        catch (Exception ex)
        {
            _logger.Warn(ex, "ClickAncestorByTextAsync('{0}', '{1}') failed", text, ancestorTag);
            return false;
        }
    }

    #endregion

    #region Time period

    /// <summary>
    /// Checks whether a <c>&lt;button data-timeperiod="..."&gt;</c> element exists,
    /// then clicks it. Returns <c>false</c> if the button is not found.
    /// </summary>
    private async Task<bool> SelectTimePeriodAsync(string timePeriod)
    {
        var exists = await TimePeriodButtonExistsAsync(timePeriod);
        if (!exists)
        {
            _logger.Debug("TimePeriod button '{0}' not found on page", timePeriod);
            return false;
        }

        await Task.Delay(_options.MinDelayMs);

        return await ClickTimePeriodButtonAsync(timePeriod);
    }

    /// <summary>
    /// Finds a <c>&lt;button data-timeperiod="..."&gt;</c> element by its
    /// <paramref name="timePeriod"/> value and clicks it.
    /// Waits <see cref="PageInteractorOptions.MinDelayMs"/> after a successful click to let the page react.
    /// </summary>
    private async Task<bool> ClickTimePeriodButtonAsync(string timePeriod)
    {
        if (_webView?.CoreWebView2 == null)
        {
            _logger.Warn("ClickTimePeriodButtonAsync('{0}') called but WebView2 is not initialized", timePeriod);
            return false;
        }

        try
        {
            var script = $@"(function() {{
  var btn = document.querySelector('button[data-timeperiod=""{timePeriod}""]');
  if (btn) {{ btn.click(); return 'clicked'; }}
  return 'not_found';
}})()";

            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            var clicked = result != "\"not_found\"";

            _logger.Debug("ClickTimePeriodButtonAsync('{0}'): {1} (raw: {2})",
                timePeriod, clicked ? "clicked" : "not_found", result);

            if (clicked)
                await Task.Delay(_options.MinDelayMs);

            return clicked;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "ClickTimePeriodButtonAsync('{0}') failed", timePeriod);
            return false;
        }
    }

    #endregion


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