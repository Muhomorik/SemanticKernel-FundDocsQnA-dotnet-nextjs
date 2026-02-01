namespace YieldRaccoon.Wpf.Models;

/// <summary>
/// Event arguments for fund data interception events.
/// </summary>
public class FundDataInterceptedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the intercepted fund data.
    /// </summary>
    public InterceptedFundList? FundData { get; set; }

    /// <summary>
    /// Gets or sets the source URI where the data was intercepted from.
    /// </summary>
    public string? SourceUri { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data was intercepted.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
