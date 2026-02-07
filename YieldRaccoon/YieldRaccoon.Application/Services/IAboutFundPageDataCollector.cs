using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Accumulates data from multiple fetch steps during a single fund detail page visit
/// and signals when all steps have resolved.
/// </summary>
/// <remarks>
/// <para><strong>Responsibility:</strong></para>
/// <para>
/// Owns the in-flight <see cref="AboutFundPageData"/> for the fund currently being visited.
/// Receives typed data slices (or failure notifications) from the infrastructure layer
/// and updates the corresponding <see cref="AboutFundFetchSlot"/>. When every slot
/// has resolved, emits on <see cref="Completed"/> so the orchestrator can persist
/// the result and advance to the next fund.
/// </para>
///
/// <para><strong>Why a separate interface?</strong></para>
/// <para>
/// Extracting collection from <see cref="IAboutFundOrchestrator"/> keeps each service
/// focused on a single concern:
/// <list type="bullet">
///   <item>The <em>orchestrator</em> manages session lifecycle, navigation sequencing, and timers.</item>
///   <item>The <em>collector</em> manages slot accumulation and completion detection.</item>
/// </list>
/// The orchestrator wires the two together — it calls <see cref="BeginCollection"/>
/// when navigating to a fund and subscribes to <see cref="Completed"/> for persistence.
/// </para>
///
/// <para><strong>Failure semantics:</strong></para>
/// <para>
/// Call <see cref="FailSlot"/> when a page interaction fails (e.g., "Inställningar"
/// button not found). The slot is marked <see cref="AboutFundFetchStatus.Failed"/>
/// and still counts toward completion — the page visit finishes even when
/// individual fetches fail.
/// </para>
///
/// <para><strong>Thread safety:</strong></para>
/// <para>
/// Implementations should be safe to call from both the UI thread
/// (page interactor failures) and background threads (intercepted responses).
/// </para>
/// </remarks>
public interface IAboutFundPageDataCollector
{
    /// <summary>
    /// Begins collecting data for a new fund page visit.
    /// Resets all slots to <see cref="AboutFundFetchStatus.Pending"/>.
    /// </summary>
    /// <param name="isin">The ISIN of the fund being visited.</param>
    /// <param name="orderBookId">The OrderBookId used in the page URL.</param>
    void BeginCollection(string isin, string orderBookId);

    /// <summary>
    /// Records a successful fetch for the <see cref="AboutFundPageData.ChartTimePeriods"/> slot.
    /// </summary>
    /// <param name="data">The raw JSON response payload.</param>
    void ReceiveChartTimePeriods(string data);

    /// <summary>
    /// Records a successful fetch for the <see cref="AboutFundPageData.SekPerformance"/> slot.
    /// </summary>
    /// <param name="data">The raw JSON response payload.</param>
    void ReceiveSekPerformance(string data);

    /// <summary>
    /// Marks a named slot as failed, recording the reason.
    /// </summary>
    /// <param name="slotName">
    /// The slot to fail — must match a property name on <see cref="AboutFundPageData"/>
    /// (e.g., <c>nameof(AboutFundPageData.ChartTimePeriods)</c>).
    /// </param>
    /// <param name="reason">Human-readable failure description.</param>
    void FailSlot(string slotName, string reason);

    /// <summary>
    /// Emits the completed <see cref="AboutFundPageData"/> when every slot has resolved
    /// (each either succeeded or failed).
    /// </summary>
    /// <remarks>
    /// Fires exactly once per <see cref="BeginCollection"/> call.
    /// The orchestrator subscribes to this to trigger a single database write
    /// and advance to the next fund.
    /// </remarks>
    IObservable<AboutFundPageData> Completed { get; }

    /// <summary>
    /// Emits the current <see cref="AboutFundPageData"/> snapshot whenever any slot changes,
    /// for UI progress display.
    /// </summary>
    IObservable<AboutFundPageData> StateChanged { get; }
}