using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NLog;
using YieldRaccoon.Wpf.Models;
using System.Text.Json.Serialization;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Intercepts WebView2 network responses to capture fund list data from API calls.
/// </summary>
/// <remarks>
/// <para>
/// Follows the same initialization pattern as <see cref="AboutFundResponseInterceptor"/>:
/// call <see cref="Initialize"/> after <c>CoreWebView2InitializationCompleted</c>.
/// </para>
/// <para>
/// Filters responses by URL pattern, parses fund list JSON, enriches with
/// pagination metadata scraped from the DOM, then raises <see cref="FundDataIntercepted"/>.
/// </para>
/// </remarks>
public class FundListResponseInterceptor : IFundListResponseInterceptor
{
    private readonly ILogger _logger;
    private WebView2? _webView;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<FundListDataInterceptedEventArgs>? FundDataIntercepted;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundListResponseInterceptor"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FundListResponseInterceptor(ILogger logger)
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

        _logger.Info("Initializing FundListResponseInterceptor");

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
            _logger.Trace("Response received: {0} - Status: {1}", e.Request.Uri, e.Response.StatusCode);

            if (ShouldInterceptResponse(e.Request.Uri))
            {
                _logger.Debug("Intercepting response from: {0}", e.Request.Uri);

                // Only process successful responses
                if (e.Response.StatusCode == 200)
                    await ProcessResponseAsync(e);
                else
                    _logger.Warn("Non-200 status code for intercepted URL: {0}", e.Response.StatusCode);
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
    internal static bool ShouldInterceptResponse(string uri)
    {
        var patterns = new[]
        {
            "/_api/fund-guide/list" // returns fundListViews JSON
        };

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

            _logger.Debug("Response content length: {0} characters", jsonContent.Length);

            // Parse JSON
            var fundData = ParseFundData(jsonContent);

            if (fundData != null)
            {
                _logger.Info("Successfully parsed fund data with {0} funds", fundData.Funds?.Count ?? 0);

                // Extract pagination info from the page DOM
                await EnrichWithPaginationMetadataAsync(fundData);

                // Raise event with intercepted data
                FundDataIntercepted?.Invoke(this, new FundListDataInterceptedEventArgs
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
    private async Task EnrichWithPaginationMetadataAsync(FundListInterceptedResponse fundData)
    {
        try
        {
            if (_webView?.CoreWebView2 == null)
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
                        "Extracted pagination info: {0} of {1}", paginationInfo.CurrentCount, paginationInfo.TotalCount);
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
    /// </summary>
    internal FundListInterceptedResponse? ParseFundData(string jsonContent)
    {
        try
        {
            var fundData = JsonSerializer.Deserialize<FundListInterceptedResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return fundData;
        }
        catch (JsonException ex)
        {
            _logger.Debug(ex, "Direct deserialization failed, trying alternative structures");

            // Try parsing as raw array
            try
            {
                var funds = JsonSerializer.Deserialize<List<FundListInterceptedFund>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (funds != null) return new FundListInterceptedResponse { Funds = funds };
            }
            catch (JsonException ex2)
            {
                _logger.Error(ex2, "Failed to parse JSON as fund list");
            }

            return null;
        }
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
            _logger.Debug("FundListResponseInterceptor disposing");

            if (_webView?.CoreWebView2 != null)
                _webView.CoreWebView2.WebResourceResponseReceived -= OnWebResourceResponseReceived;

            _webView = null;
        }

        _disposed = true;
    }
}
