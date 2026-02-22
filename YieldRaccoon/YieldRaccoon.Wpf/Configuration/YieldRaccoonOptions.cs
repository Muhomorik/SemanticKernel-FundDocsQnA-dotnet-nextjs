namespace YieldRaccoon.Wpf.Configuration;

/// <summary>
/// Configuration options for the YieldRaccoon application.
/// Loaded from User Secrets (development) or Azure Key Vault (production).
/// </summary>
public class YieldRaccoonOptions
{
    /// <summary>
    /// Gets or sets the URL to the fund list/search page with overview tab selected.
    /// </summary>
    /// <example>https://www.&lt;fund-provider&gt;.com/funds/list?tab=overview</example>
    public string FundListPageUrlOverviewTab { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL template for fund details pages.
    /// Use {isin} as a placeholder for the fund's ISIN code.
    /// </summary>
    /// <example>https://www.&lt;fund-provider&gt;.com/fund/{isin}</example>
    public string FundDetailsPageUrlTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the about-fund overview should auto-start navigating through funds.
    /// </summary>
    /// <remarks>
    /// When true, the browsing session starts automatically when the AboutFund window opens.
    /// Default: false (user must click "Start Overview" button).
    /// </remarks>
    public bool AutoStartOverview { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use minimal delays for page interactions and browsing pauses.
    /// </summary>
    /// <remarks>
    /// When true, uses short delays (3-7s clicks, 2s panel animations, 3-8s between pages)
    /// instead of the normal human-like timings. Useful for development and testing.
    /// Default: false.
    /// </remarks>
    public bool FastMode { get; set; } = false;

    /// <summary>
    /// Gets the fund details URL for a specific ISIN.
    /// </summary>
    /// <param name="isin">The fund's ISIN code.</param>
    /// <returns>The formatted URL with the ISIN substituted.</returns>
    public string GetFundDetailsUrl(string isin)
    {
        if (string.IsNullOrWhiteSpace(isin))
            throw new ArgumentException("ISIN cannot be null or whitespace.", nameof(isin));

        return FundDetailsPageUrlTemplate.Replace("{isin}", isin, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the fund details URL for a specific OrderbookId.
    /// </summary>
    /// <remarks>
    /// Uses the <c>{0}</c> placeholder format from user secrets.
    /// The external website uses OrderbookId in the URL, while internal tracking uses ISIN.
    /// </remarks>
    /// <param name="orderbookId">The fund's OrderbookId.</param>
    /// <returns>The formatted URL with the OrderbookId substituted.</returns>
    public string GetFundDetailsUrlByOrderbookId(string orderbookId)
    {
        if (string.IsNullOrWhiteSpace(orderbookId))
            throw new ArgumentException("OrderbookId cannot be null or whitespace.", nameof(orderbookId));

        return FundDetailsPageUrlTemplate.Replace("{0}", orderbookId, StringComparison.OrdinalIgnoreCase);
    }
}