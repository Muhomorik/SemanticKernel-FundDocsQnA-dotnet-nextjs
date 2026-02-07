namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Represents the outcome state of a single data-fetch step within an about-fund page visit.
/// </summary>
/// <remarks>
/// <para>
/// Each fund detail page requires multiple interactions (button clicks) that each trigger
/// an API call whose response must be captured. Every such step independently resolves
/// to either <see cref="Succeeded"/> or <see cref="Failed"/>.
/// </para>
/// <para>
/// A step is considered <em>resolved</em> once it leaves <see cref="Pending"/> —
/// regardless of whether it succeeded or failed. This allows the parent
/// <c>AboutFundPageData</c> to treat the page visit as complete even when
/// individual steps fail (e.g., a button was not found on the page).
/// </para>
/// </remarks>
public enum AboutFundFetchStatus
{
    /// <summary>
    /// The step has not yet been attempted or its response has not arrived.
    /// </summary>
    Pending,

    /// <summary>
    /// The step completed successfully and its data was captured.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The step failed — the page element was not found, the API call timed out,
    /// or the response could not be parsed.
    /// </summary>
    Failed
}
