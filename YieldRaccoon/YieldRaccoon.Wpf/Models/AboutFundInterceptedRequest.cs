namespace YieldRaccoon.Wpf.Models;

/// <summary>
/// Model representing an intercepted HTTP request/response pair from the AboutFund browser.
/// </summary>
public class AboutFundInterceptedRequest
{
    /// <summary>
    /// Gets the unique identifier for this request.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Gets the timestamp when the response was received.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// Gets the HTTP method (GET, POST, etc.).
    /// </summary>
    public string Method { get; init; } = "GET";

    /// <summary>
    /// Gets the request URL.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets the reason phrase of the response (e.g., "OK", "Not Found").
    /// </summary>
    public string StatusText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the Content-Type header value.
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the Content-Length in bytes.
    /// </summary>
    public long ContentLength { get; init; }

    /// <summary>
    /// Gets a preview of the response content (first 2KB for JSON/text responses).
    /// </summary>
    public string? ResponsePreview { get; init; }
}
