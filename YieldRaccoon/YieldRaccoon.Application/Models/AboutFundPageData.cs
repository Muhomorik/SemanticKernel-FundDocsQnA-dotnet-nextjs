using System.Diagnostics;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Models;

/// <summary>
/// Tracks the data collection state for a single fund detail page visit.
/// </summary>
/// <remarks>
/// <para>
/// When the orchestrator navigates to a fund's detail page, multiple interactions
/// (button clicks) trigger separate API calls whose responses must be captured.
/// This record acts as an accumulator — each slot starts as
/// <see cref="AboutFundFetchStatus.Pending"/> and independently resolves to
/// <see cref="AboutFundFetchStatus.Succeeded"/> or <see cref="AboutFundFetchStatus.Failed"/>.
/// </para>
///
/// <para><strong>Completion semantics:</strong></para>
/// <para>
/// <see cref="IsComplete"/> becomes <c>true</c> when <em>every</em> slot is resolved,
/// regardless of individual success or failure. A page visit where "Inställningar"
/// was not found is still considered complete — the slot is marked
/// <see cref="AboutFundFetchStatus.Failed"/> and the orchestrator can advance
/// to the next fund without waiting forever.
/// </para>
///
/// <para><strong>Immutability:</strong></para>
/// <para>
/// This is an immutable <c>record</c>. To update a slot, create a new instance
/// via <c>with</c> expression:
/// </para>
/// <code>
/// pageData = pageData with { ChartTimePeriods = AboutFundFetchSlot.Succeeded(json) };
/// </code>
///
/// <para><strong>Lifecycle:</strong></para>
/// <list type="number">
///   <item>Created by <c>IAboutFundPageDataCollector.BeginCollection</c> when navigation starts (all slots pending).</item>
///   <item>Updated as intercepted responses arrive or page interactions fail.</item>
///   <item>Once <see cref="IsComplete"/> is <c>true</c>, persisted to database as a single write.</item>
/// </list>
/// </remarks>
[DebuggerDisplay(
    "PageData: {Isin} | Chart={ChartTimePeriods.Status}, SEK={SekPerformance.Status} | Complete={IsComplete}")]
public sealed record AboutFundPageData
{
    /// <summary>
    /// Gets the ISIN identifier of the fund being visited.
    /// </summary>
    public required string Isin { get; init; }

    /// <summary>
    /// Gets the OrderBookId used in the external URL for this fund.
    /// </summary>
    public required string OrderBookId { get; init; }

    // ── Fetch slots ───────────────────────────────────────────────

    /// <summary>
    /// Gets the fetch outcome for chart time-period data,
    /// triggered by navigating to the fund detail page.
    /// </summary>
    /// <remarks>
    /// Populated from the <c>chart/timeperiods/{orderbookId}</c> API response
    /// intercepted after the page loads.
    /// </remarks>
    public AboutFundFetchSlot ChartTimePeriods { get; init; } = AboutFundFetchSlot.Pending();

    /// <summary>
    /// Gets the fetch outcome for SEK performance data,
    /// triggered by clicking the "Utvecklingen i SEK" checkbox.
    /// </summary>
    /// <remarks>
    /// Populated from the API response intercepted after the page interactor
    /// clicks "Inställningar" → "Utvecklingen i SEK". If the button is not
    /// found on the page, this slot is marked <see cref="AboutFundFetchStatus.Failed"/>.
    /// </remarks>
    public AboutFundFetchSlot SekPerformance { get; init; } = AboutFundFetchSlot.Pending();

    // ── Completion checks ─────────────────────────────────────────

    /// <summary>
    /// Gets a value indicating whether all fetch slots have resolved
    /// (each is either <see cref="AboutFundFetchStatus.Succeeded"/> or
    /// <see cref="AboutFundFetchStatus.Failed"/>).
    /// </summary>
    /// <remarks>
    /// The orchestrator should persist page data and advance to the next fund
    /// once this returns <c>true</c>. Failed slots do not block completion.
    /// </remarks>
    public bool IsComplete =>
        ChartTimePeriods.IsResolved
        && SekPerformance.IsResolved;

    /// <summary>
    /// Gets a value indicating whether every slot succeeded with data.
    /// </summary>
    /// <remarks>
    /// Useful for reporting and diagnostics — distinguishes a fully successful
    /// page visit from one that completed with partial failures.
    /// </remarks>
    public bool IsFullySuccessful =>
        ChartTimePeriods.IsSucceeded
        && SekPerformance.IsSucceeded;

    /// <summary>
    /// Gets the number of slots that have resolved (succeeded or failed).
    /// </summary>
    public int ResolvedCount =>
        (ChartTimePeriods.IsResolved ? 1 : 0)
        + (SekPerformance.IsResolved ? 1 : 0);

    /// <summary>
    /// Gets the total number of fetch slots tracked for this page visit.
    /// </summary>
    public int TotalSlots => 2;
}