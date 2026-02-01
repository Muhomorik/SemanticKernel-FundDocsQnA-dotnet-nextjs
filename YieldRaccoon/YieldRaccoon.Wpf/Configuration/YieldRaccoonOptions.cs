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
}