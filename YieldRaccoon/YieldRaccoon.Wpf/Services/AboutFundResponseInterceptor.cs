using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Intercepts all text-based WebView2 network responses
/// and forwards captured data to the <see cref="IAboutFundPageDataCollector"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service bridges the Presentation layer (WebView2) with the Application layer (collector)
/// following DDD dependency direction: Presentation → Application.
/// </para>
/// <para>
/// On each text-based response:
/// <list type="number">
///   <item>Raises <see cref="RequestIntercepted"/> for UI consumers (e.g., network inspector panel)</item>
///   <item>Calls <see cref="IAboutFundPageDataCollector.NotifyResponseCaptured"/> directly</item>
/// </list>
/// </para>
/// <para>
/// The <see cref="RequestIntercepted"/> event handler and <see cref="IAboutFundPageDataCollector.NotifyResponseCaptured"/>
/// may be called from a WebView2 background thread — subscribers requiring UI thread access must marshal accordingly.
/// </para>
/// </remarks>
public class AboutFundResponseInterceptor : IAboutFundResponseInterceptor
{
    private readonly ILogger _logger;
    private readonly IAboutFundPageDataCollector _collector;
    private WebView2? _webView;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<AboutFundInterceptedRequest>? RequestIntercepted;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundResponseInterceptor"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="collector">The collector for routing intercepted responses to data slots.</param>
    public AboutFundResponseInterceptor(ILogger logger, IAboutFundPageDataCollector collector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }

    /// <inheritdoc />
    public void Initialize(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));

        if (_webView.CoreWebView2 == null)
            throw new InvalidOperationException(
                "WebView2 CoreWebView2 must be initialized before calling Initialize()");

        _logger.Info("Initializing AboutFundResponseInterceptor");

        // Subscribe to response received event
        _webView.CoreWebView2.WebResourceResponseReceived += OnWebResourceResponseReceived;

        _logger.Debug("WebResourceResponseReceived event handler attached");
    }

    /// <summary>
    /// Handles web resource response received events, forwarding text-based responses to the collector.
    /// </summary>
    private async void OnWebResourceResponseReceived(
        object? sender,
        CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        try
        {
            if (ShouldSkipRequest(e.Request.Uri))
                return;

            _logger.Trace("Response received: {0} {1} - Status: {2}",
                e.Request.Method, e.Request.Uri, e.Response.StatusCode);

            await ProcessResponseAsync(e);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in OnWebResourceResponseReceived");
        }
    }

    /// <summary>
    /// Processes the intercepted response and creates an InterceptedRequest model.
    /// Skips responses that have no text-based content.
    /// </summary>
    private async Task ProcessResponseAsync(CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        string? responseBody = null;

        try
        {
            var contentType = GetHeader(e.Response.Headers, "Content-Type") ?? string.Empty;

            if (IsTextBasedContent(contentType)) responseBody = await ExtractResponseBodyAsync(e);
        }
        catch (COMException ex)
        {
            _logger.Trace(ex, "Could not read response content (may have been consumed)");
        }
        catch (Exception ex)
        {
            _logger.Trace(ex, "Error extracting response body");
        }

        // Only create intercepted request models for responses with content
        if (string.IsNullOrEmpty(responseBody))
            return;

        var interceptedRequest = new AboutFundInterceptedRequest
        {
            Timestamp = DateTime.Now,
            Method = e.Request.Method,
            Url = new Uri(e.Request.Uri),
            StatusCode = e.Response.StatusCode,
            StatusText = e.Response.ReasonPhrase,
            ContentType = GetHeader(e.Response.Headers, "Content-Type") ?? string.Empty,
            ContentLength = ParseContentLength(GetHeader(e.Response.Headers, "Content-Length")),
            ResponseBody = responseBody
        };

        // Raise event for UI consumers
        RequestIntercepted?.Invoke(this, interceptedRequest);

        // Route captured response to collector for slot matching
        _collector.NotifyResponseCaptured(interceptedRequest);
    }

    /// <summary>
    /// Reads the full response body as a string.
    /// </summary>
    private static async Task<string?> ExtractResponseBodyAsync(CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        var contentStream = await e.Response.GetContentAsync();

        if (contentStream == null)
            return null;

        using var reader = new StreamReader(contentStream);
        var content = await reader.ReadToEndAsync();

        return content.Length == 0 ? null : content;
    }

    /// <summary>
    /// Returns <c>true</c> for requests that should be silently ignored —
    /// data URIs and common image/font/media assets that are irrelevant to data collection.
    /// </summary>
    private static bool ShouldSkipRequest(string uri)
    {
        if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return true;

        // Strip query string / fragment before checking extension
        var path = uri.AsSpan();
        var queryIndex = path.IndexOfAny('?', '#');
        if (queryIndex >= 0)
            path = path[..queryIndex];

        return path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".eot", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the content type is text-based and should have a preview extracted.
    /// </summary>
    private static bool IsTextBasedContent(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var textTypes = new[]
        {
            "application/json",
            "application/javascript",
            "application/xml",
            "text/",
            "application/x-www-form-urlencoded"
        };

        return textTypes.Any(t => contentType.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a header value from the response headers.
    /// </summary>
    private static string? GetHeader(CoreWebView2HttpResponseHeaders headers, string name)
    {
        try
        {
            return headers.GetHeader(name);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses Content-Length header to long.
    /// </summary>
    private static long ParseContentLength(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return long.TryParse(value, out var length) ? length : 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _logger.Debug("AboutFundResponseInterceptor disposing");

            if (_webView?.CoreWebView2 != null)
                _webView.CoreWebView2.WebResourceResponseReceived -= OnWebResourceResponseReceived;

            _webView = null;
        }

        _disposed = true;
    }
}