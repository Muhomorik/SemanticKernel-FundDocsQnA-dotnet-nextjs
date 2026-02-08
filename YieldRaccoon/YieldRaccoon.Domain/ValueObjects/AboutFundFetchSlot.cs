using System.Diagnostics;

namespace YieldRaccoon.Domain.ValueObjects;

/// <summary>
/// Immutable outcome of a single data-fetch step during an about-fund page visit.
/// </summary>
/// <remarks>
/// <para>
/// Each fund detail page triggers multiple API calls (e.g., chart time periods,
/// SEK performance). Each call is tracked as an independent slot that transitions
/// from <see cref="AboutFundFetchStatus.Pending"/> to either
/// <see cref="AboutFundFetchStatus.Succeeded"/> or <see cref="AboutFundFetchStatus.Failed"/>.
/// </para>
/// <para>
/// A slot is <em>resolved</em> once it is no longer <see cref="AboutFundFetchStatus.Pending"/>.
/// Failed slots are still considered resolved — this allows the parent aggregate
/// to treat the page visit as complete even when individual fetches fail
/// (e.g., the "Inställningar" button was not found on the page).
/// </para>
/// <para>
/// Slots are immutable records. To transition state, use the static factory methods
/// <see cref="Succeeded"/> or <see cref="Failed"/> to create a new instance, then
/// replace the slot in the parent <c>AboutFundPageData</c> via <c>with</c> expression.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A slot starts as pending:
/// var slot = AboutFundFetchSlot.Pending();
///
/// // On success, replace with captured data:
/// slot = AboutFundFetchSlot.Succeeded("{ \"periods\": [...] }");
///
/// // On failure (element not found, timeout, parse error):
/// slot = AboutFundFetchSlot.Failed("'Inställningar' button not found on page");
/// </code>
/// </example>
[DebuggerDisplay("{Status}, Data={Data != null ? Data.Length + \" chars\" : \"null\"}, Reason={FailureReason}")]
public sealed record AboutFundFetchSlot
{
    /// <summary>
    /// Gets the current state of this fetch step.
    /// </summary>
    public AboutFundFetchStatus Status { get; private init; } = AboutFundFetchStatus.Pending;

    /// <summary>
    /// Gets the raw captured response data when <see cref="Status"/> is
    /// <see cref="AboutFundFetchStatus.Succeeded"/>; <c>null</c> otherwise.
    /// </summary>
    public string? Data { get; private init; }

    /// <summary>
    /// Gets the human-readable failure reason when <see cref="Status"/> is
    /// <see cref="AboutFundFetchStatus.Failed"/>; <c>null</c> otherwise.
    /// </summary>
    public string? FailureReason { get; private init; }

    /// <summary>
    /// Gets a value indicating whether this slot has resolved
    /// (either succeeded or failed — no longer pending).
    /// </summary>
    public bool IsResolved => Status != AboutFundFetchStatus.Pending;

    /// <summary>
    /// Gets a value indicating whether this slot completed successfully with data.
    /// </summary>
    public bool IsSucceeded => Status == AboutFundFetchStatus.Succeeded;

    /// <summary>
    /// Creates a new slot in the <see cref="AboutFundFetchStatus.Pending"/> state.
    /// </summary>
    public static AboutFundFetchSlot Pending() => new();

    /// <summary>
    /// Creates a new slot in the <see cref="AboutFundFetchStatus.Succeeded"/> state
    /// with captured response data.
    /// </summary>
    /// <param name="data">The raw response payload (typically JSON).</param>
    public static AboutFundFetchSlot Succeeded(string data) =>
        new() { Status = AboutFundFetchStatus.Succeeded, Data = data };

    /// <summary>
    /// Creates a new slot in the <see cref="AboutFundFetchStatus.Failed"/> state
    /// with a reason describing what went wrong.
    /// </summary>
    /// <param name="reason">
    /// Human-readable description of the failure
    /// (e.g., "button not found", "response timeout", "JSON parse error").
    /// </param>
    public static AboutFundFetchSlot Failed(string reason) =>
        new() { Status = AboutFundFetchStatus.Failed, FailureReason = reason };
}
