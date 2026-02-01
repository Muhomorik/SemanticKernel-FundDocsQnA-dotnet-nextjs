using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NLog;
using YieldRaccoon.Wpf.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service that intercepts WebView2 network responses to capture fund data from JavaScript API calls.
/// </summary>
public class WebView2ResponseInterceptor : IDisposable
{
    private readonly ILogger _logger;
    private readonly WebView2 _webView;
    private bool _disposed;

    /// <summary>
    /// Event raised when fund list data is intercepted from a network response.
    /// </summary>
    public event EventHandler<FundDataInterceptedEventArgs>? FundDataIntercepted;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebView2ResponseInterceptor"/> class.
    /// </summary>
    /// <param name="webView">The WebView2 control to monitor.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public WebView2ResponseInterceptor(WebView2 webView, ILogger logger)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the interceptor and starts monitoring network responses.
    /// Must be called after WebView2 CoreWebView2 is initialized.
    /// </summary>
    public void Initialize()
    {
        if (_webView.CoreWebView2 == null)
            throw new InvalidOperationException(
                "WebView2 CoreWebView2 must be initialized before calling Initialize()");

        _logger.Info("Initializing WebView2ResponseInterceptor");

        // Subscribe to response received event
        _webView.CoreWebView2.WebResourceResponseReceived += OnWebResourceResponseReceived;

        _logger.Debug("WebResourceResponseReceived event handler attached");
    }

    /// <summary>
    /// Handles web resource response received events to intercept fund data.
    /// </summary>
    private async void OnWebResourceResponseReceived(
        object? sender,
        CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        try
        {
            // Log all requests for debugging (can be removed in production)
            _logger.Trace($"Response received: {e.Request.Uri} - Status: {e.Response.StatusCode}");

            // Filter by URL pattern - adjust these patterns to match your actual API endpoints
            if (ShouldInterceptResponse(e.Request.Uri))
            {
                _logger.Debug($"Intercepting response from: {e.Request.Uri}");

                // Only process successful responses
                if (e.Response.StatusCode == 200)
                    await ProcessResponseAsync(e);
                else
                    _logger.Warn($"Non-200 status code for intercepted URL: {e.Response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in OnWebResourceResponseReceived");
        }
    }

    /// <summary>
    /// Determines whether a response should be intercepted based on URL patterns.
    /// </summary>
    /// <param name="uri">The request URI.</param>
    /// <returns>True if the response should be intercepted; otherwise, false.</returns>
    private bool ShouldInterceptResponse(string uri)
    {
        // Add your URL patterns here
        // Examples:
        // - API endpoint: uri.Contains("/api/funds")
        // - JavaScript function: uri.Contains("getFundList")
        // - Specific domain: uri.StartsWith("https://api.yoursite.com/funds")

        var patterns = new[]
        {
            "/_api/fund-guide/list" // returns fundListViews JSON
            // Add more fund API endpoints here as needed
        };

        _logger.Trace($"Checking if URI should be intercepted: {uri}");

        return patterns.Any(pattern => uri.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Processes the intercepted response and extracts fund data.
    /// </summary>
    private async Task ProcessResponseAsync(CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        try
        {
            // Get response content as stream
            var contentStream = await e.Response.GetContentAsync();

            if (contentStream == null)
            {
                _logger.Warn("Response content is null");
                return;
            }

            // Read JSON content
            using var reader = new StreamReader(contentStream);
            var jsonContent = await reader.ReadToEndAsync();

            _logger.Debug($"Response content length: {jsonContent.Length} characters");

            // Parse JSON
            var fundData = ParseFundData(jsonContent);

            if (fundData != null)
            {
                _logger.Info($"Successfully parsed fund data with {fundData.Funds?.Count ?? 0} funds");

                // Extract pagination info from the page DOM
                await EnrichWithPaginationMetadataAsync(fundData);

                // Raise event with intercepted data
                FundDataIntercepted?.Invoke(this, new FundDataInterceptedEventArgs
                {
                    FundData = fundData,
                    SourceUri = e.Request.Uri,
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                _logger.Warn("Failed to parse fund data from response");
            }
        }
        catch (COMException ex)
        {
            _logger.Error(ex, "COM exception while reading response content (content may have been consumed already)");
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "JSON parsing error");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing response");
        }
    }

    /// <summary>
    /// Extracts pagination metadata from the page DOM and enriches the fund data.
    /// Parses Swedish text like "Visar 20 av 1462 i ditt filtreringsresultat".
    /// </summary>
    private async Task EnrichWithPaginationMetadataAsync(InterceptedFundList fundData)
    {
        try
        {
            if (_webView.CoreWebView2 == null)
            {
                _logger.Debug("CoreWebView2 not available, skipping pagination metadata extraction");
                return;
            }

            // JavaScript to extract pagination info from the page
            var paginationScript = @"
                (function() {
                    // Look for Swedish pagination text: 'Visar X av Y i ditt filtreringsresultat'
                    const bodyText = document.body.innerText;

                    // Match pattern: 'Visar <number> av <number>'
                    const match = bodyText.match(/Visar\s+(\d+)\s+av\s+(\d+)/i);

                    if (match) {
                        return JSON.stringify({
                            currentCount: parseInt(match[1], 10),
                            totalCount: parseInt(match[2], 10)
                        });
                    }

                    // Alternative: Look for specific elements that might contain this info
                    const paginationElements = document.querySelectorAll('[class*=""pagination""], [class*=""result""], [class*=""count""]');
                    for (const elem of paginationElements) {
                        const text = elem.textContent || '';
                        const m = text.match(/Visar\s+(\d+)\s+av\s+(\d+)/i);
                        if (m) {
                            return JSON.stringify({
                                currentCount: parseInt(m[1], 10),
                                totalCount: parseInt(m[2], 10)
                            });
                        }
                    }

                    return null;
                })();
            ";

            var result = await _webView.CoreWebView2.ExecuteScriptAsync(paginationScript);

            // ExecuteScriptAsync returns JSON-encoded string, so we need to decode it
            if (!string.IsNullOrEmpty(result) && result != "null")
            {
                // Remove surrounding quotes if present
                var jsonResult = result.Trim('"').Replace("\\\"", "\"");

                var paginationInfo = JsonSerializer.Deserialize<PaginationInfo>(jsonResult);

                if (paginationInfo != null)
                {
                    fundData.CurrentCount = paginationInfo.CurrentCount;
                    fundData.TotalCount = paginationInfo.TotalCount;

                    _logger.Info(
                        $"Extracted pagination info: {paginationInfo.CurrentCount} of {paginationInfo.TotalCount}");
                }
            }
            else
            {
                _logger.Debug("No pagination metadata found on page");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error extracting pagination metadata");
            // Don't throw - pagination metadata is optional
        }
    }

    /// <summary>
    /// Helper class for deserializing pagination info from JavaScript.
    /// </summary>
    private class PaginationInfo
    {
        [JsonPropertyName("currentCount")] public int CurrentCount { get; set; }

        [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    }


    /// <summary>
    /// Parses fund data from JSON content.
    /// Adjust this method based on your actual API response structure.
    /// </summary>
    private InterceptedFundList? ParseFundData(string jsonContent)
    {
        try
        {
            // Option 1: Direct deserialization if response matches InterceptedFundList structure
            var fundData = JsonSerializer.Deserialize<InterceptedFundList>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return fundData;
        }
        catch (JsonException ex)
        {
            _logger.Debug(ex, "Direct deserialization failed, trying alternative structures");

            // Option 2: Try parsing as raw array
            try
            {
                var funds = JsonSerializer.Deserialize<List<InterceptedFund>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (funds != null) return new InterceptedFundList { Funds = funds };
            }
            catch (JsonException ex2)
            {
                _logger.Error(ex2, "Failed to parse JSON as fund list");
            }

            return null;
        }
    }

    /// <summary>
    /// Releases all resources used by the interceptor.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _logger.Debug("WebView2ResponseInterceptor disposing");

            // Unsubscribe from events
            if (_webView?.CoreWebView2 != null)
                _webView.CoreWebView2.WebResourceResponseReceived -= OnWebResourceResponseReceived;
        }

        _disposed = true;
    }
}