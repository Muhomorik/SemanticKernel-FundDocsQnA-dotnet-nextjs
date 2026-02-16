namespace YieldRaccoon.Application.Configuration;

/// <summary>
/// Configuration for building fund detail page URLs.
/// </summary>
/// <param name="UrlTemplate">
/// URL template with <c>{0}</c> placeholder for the OrderBookId
/// (e.g., <c>https://&lt;fund-provider&gt;.com/fund/{0}</c>).
/// </param>
public record FundDetailsUrlBuilderOptions(string UrlTemplate);
