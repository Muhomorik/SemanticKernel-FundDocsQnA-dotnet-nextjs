using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NLog;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service that intercepts ALL WebView2 network responses for debugging and exploration.
/// Unlike <see cref="WebView2ResponseInterceptor"/>, this captures every request without URL filtering.
/// </summary>
public class AboutFundResponseInterceptor : IAboutFundResponseInterceptor
{
    private readonly ILogger _logger;
    private WebView2? _webView;
    private bool _disposed;

    /// <summary>
    /// Maximum response content preview size in characters.
    /// </summary>
    private const int MaxPreviewLength = 2048;

    /// <inheritdoc />
    public event EventHandler<AboutFundInterceptedRequest>? RequestIntercepted;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundResponseInterceptor"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AboutFundResponseInterceptor(ILogger logger)
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

        _logger.Info("Initializing AboutFundResponseInterceptor");

        // Subscribe to response received event
        _webView.CoreWebView2.WebResourceResponseReceived += OnWebResourceResponseReceived;

        _logger.Debug("WebResourceResponseReceived event handler attached");
    }

    /// <summary>
    /// Handles web resource response received events to capture all network traffic.
    /// </summary>
    private async void OnWebResourceResponseReceived(
        object? sender,
        CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        try
        {
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
    /// </summary>
    private async Task ProcessResponseAsync(CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        string? responsePreview = null;

        try
        {
            // Try to extract response content preview for JSON/text responses
            var contentType = GetHeader(e.Response.Headers, "Content-Type") ?? string.Empty;

            if (IsTextBasedContent(contentType))
            {
                responsePreview = await ExtractResponsePreviewAsync(e);
            }
        }
        catch (COMException ex)
        {
            _logger.Trace(ex, "Could not read response content (may have been consumed)");
        }
        catch (Exception ex)
        {
            _logger.Trace(ex, "Error extracting response preview");
        }

        // Create intercepted request model
        var interceptedRequest = new AboutFundInterceptedRequest
        {
            Timestamp = DateTime.Now,
            Method = e.Request.Method,
            Url = e.Request.Uri,
            StatusCode = e.Response.StatusCode,
            StatusText = e.Response.ReasonPhrase,
            ContentType = GetHeader(e.Response.Headers, "Content-Type") ?? string.Empty,
            ContentLength = ParseContentLength(GetHeader(e.Response.Headers, "Content-Length")),
            ResponsePreview = responsePreview
        };

        // Raise event
        RequestIntercepted?.Invoke(this, interceptedRequest);
    }

    /// <summary>
    /// Extracts a preview of the response content (up to MaxPreviewLength characters).
    /// </summary>
    private async Task<string?> ExtractResponsePreviewAsync(CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        var contentStream = await e.Response.GetContentAsync();

        if (contentStream == null)
            return null;

        using var reader = new StreamReader(contentStream);
        var buffer = new char[MaxPreviewLength];
        var charsRead = await reader.ReadAsync(buffer, 0, MaxPreviewLength);

        if (charsRead == 0)
            return null;

        var content = new string(buffer, 0, charsRead);

        // Indicate if content was truncated
        if (charsRead == MaxPreviewLength && !reader.EndOfStream)
        {
            content += "...";
        }

        return content;
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
            {
                _webView.CoreWebView2.WebResourceResponseReceived -= OnWebResourceResponseReceived;
            }

            _webView = null;
        }

        _disposed = true;
    }
}
