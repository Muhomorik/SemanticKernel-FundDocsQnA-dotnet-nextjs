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
/// regardless of individual success or failure. A page visit where a button
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
/// pageData = pageData with { Chart1Month = AboutFundFetchSlot.Succeeded(json) };
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
    "PageData: {OrderBookId} | " +
    "1M={Chart1Month.Status}, 3M={Chart3Months.Status}, YTD={ChartYearToDate.Status}, " +
    "1Y={Chart1Year.Status}, 3Y={Chart3Years.Status}, 5Y={Chart5Years.Status}, Max={ChartMax.Status} | " +
    "Complete={IsComplete}")]
public sealed record AboutFundPageData
{
    /// <summary>
    /// Gets the OrderBookId used in the external URL for this fund.
    /// </summary>
    public required OrderBookId OrderBookId { get; init; }

    #region Fetch slots (in call order)

    /// <summary>
    /// Step 1 — Chart data after selecting the "1 month" time period.
    /// </summary>
    public AboutFundFetchSlot Chart1Month { get; init; } = AboutFundFetchSlot.Pending();

    /// <summary>
    /// Step 2 — Chart data after selecting the "3 months" time period.
    /// </summary>
    public AboutFundFetchSlot Chart3Months { get; init; } = AboutFundFetchSlot.Pending();

    /// <summary>
    /// Step 3 — Chart data after selecting the "year to date" time period.
    /// </summary>
    public AboutFundFetchSlot ChartYearToDate { get; init; } = AboutFundFetchSlot.Pending();

    /// <summary>
    /// Step 4 — Chart data after selecting the "1 year" time period.
    /// </summary>
    public AboutFundFetchSlot Chart1Year { get; init; } = AboutFundFetchSlot.Pending();

    /// <summary>
    /// Step 5 — Chart data after selecting the "3 years" time period.
    /// </summary>
    public AboutFundFetchSlot Chart3Years { get; init; } = AboutFundFetchSlot.Pending();

    /// <summary>
    /// Step 6 — Chart data after selecting the "5 years" time period.
    /// </summary>
    public AboutFundFetchSlot Chart5Years { get; init; } = AboutFundFetchSlot.Pending();

    /// <summary>
    /// Step 7 — Chart data after selecting the "max" time period.
    /// </summary>
    public AboutFundFetchSlot ChartMax { get; init; } = AboutFundFetchSlot.Pending();

    #endregion

    #region Completion checks

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
        Chart1Month.IsResolved
        && Chart3Months.IsResolved
        && ChartYearToDate.IsResolved
        && Chart1Year.IsResolved
        && Chart3Years.IsResolved
        && Chart5Years.IsResolved
        && ChartMax.IsResolved;

    /// <summary>
    /// Gets a value indicating whether every slot succeeded with data.
    /// </summary>
    /// <remarks>
    /// Useful for reporting and diagnostics — distinguishes a fully successful
    /// page visit from one that completed with partial failures.
    /// </remarks>
    public bool IsFullySuccessful =>
        Chart1Month.IsSucceeded
        && Chart3Months.IsSucceeded
        && ChartYearToDate.IsSucceeded
        && Chart1Year.IsSucceeded
        && Chart3Years.IsSucceeded
        && Chart5Years.IsSucceeded
        && ChartMax.IsSucceeded;

    /// <summary>
    /// Gets the number of slots that have resolved (succeeded or failed).
    /// </summary>
    public int ResolvedCount =>
        (Chart1Month.IsResolved ? 1 : 0)
        + (Chart3Months.IsResolved ? 1 : 0)
        + (ChartYearToDate.IsResolved ? 1 : 0)
        + (Chart1Year.IsResolved ? 1 : 0)
        + (Chart3Years.IsResolved ? 1 : 0)
        + (Chart5Years.IsResolved ? 1 : 0)
        + (ChartMax.IsResolved ? 1 : 0);

    /// <summary>
    /// Gets the total number of fetch slots tracked for this page visit.
    /// </summary>
    public int TotalSlots => 7;

    /// <summary>
    /// Returns all 7 fetch slots as an ordered enumerable of (slot identifier, slot data) pairs.
    /// </summary>
    public IEnumerable<(AboutFundDataSlot Slot, AboutFundFetchSlot Data)> AllSlots()
    {
        yield return (AboutFundDataSlot.Chart1Month, Chart1Month);
        yield return (AboutFundDataSlot.Chart3Months, Chart3Months);
        yield return (AboutFundDataSlot.ChartYearToDate, ChartYearToDate);
        yield return (AboutFundDataSlot.Chart1Year, Chart1Year);
        yield return (AboutFundDataSlot.Chart3Years, Chart3Years);
        yield return (AboutFundDataSlot.Chart5Years, Chart5Years);
        yield return (AboutFundDataSlot.ChartMax, ChartMax);
    }

    #endregion
}
