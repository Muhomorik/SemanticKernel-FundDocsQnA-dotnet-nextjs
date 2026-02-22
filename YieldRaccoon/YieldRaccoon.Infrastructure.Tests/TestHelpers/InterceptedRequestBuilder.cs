using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Fluent builder for <see cref="AboutFundInterceptedRequest"/> test instances.
/// </summary>
public class InterceptedRequestBuilder
{
    private Uri _url = new("https://example.com/api/chart?period=1m");
    private int _statusCode = 200;
    private string _statusText = "OK";
    private string _responseBody = """{ "data": [] }""";

    public InterceptedRequestBuilder WithUrl(string url)
    {
        _url = new Uri(url);
        return this;
    }

    public InterceptedRequestBuilder WithStatusCode(int code, string text = "")
    {
        _statusCode = code;
        _statusText = text;
        return this;
    }

    public InterceptedRequestBuilder WithResponseBody(string body)
    {
        _responseBody = body;
        return this;
    }

    /// <summary>
    /// Creates a builder pre-configured to match the given slot
    /// using <see cref="TestEndpointPatterns.SlotFragments"/>.
    /// </summary>
    public static InterceptedRequestBuilder ForSlot(AboutFundDataSlot slot)
    {
        var fragment = TestEndpointPatterns.SlotFragments[slot];
        return new InterceptedRequestBuilder()
            .WithUrl($"https://example.com/api/chart?{fragment}");
    }

    /// <summary>
    /// Creates a builder with a URL that matches no configured patterns.
    /// </summary>
    public static InterceptedRequestBuilder Unmatched() =>
        new InterceptedRequestBuilder()
            .WithUrl("https://example.com/unrelated/endpoint");

    public AboutFundInterceptedRequest Build() => new()
    {
        Url = _url,
        StatusCode = _statusCode,
        StatusText = _statusText,
        ResponseBody = _responseBody,
    };
}
