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
/// Internally handles response routing, page interactions, slot accumulation,
/// and completion detection. External callers only need to start a collection
/// and forward intercepted HTTP responses.
/// </para>
///
/// <para><strong>Scheduling:</strong></para>
/// <para>
/// Step timings are pre-calculated by the orchestrator and passed via
/// <see cref="AboutFundCollectionSchedule"/>. The collector schedules timers at the
/// prescribed absolute times — it does not calculate delays itself.
/// </para>
///
/// <para><strong>Thread safety:</strong></para>
/// <para>
/// Implementations should be safe to call from both the UI thread
/// and background threads (intercepted responses).
/// </para>
/// </remarks>
public interface IAboutFundPageDataCollector
{
    /// <summary>
    /// Emits the completed <see cref="AboutFundPageData"/> when the page visit finishes
    /// (after all interactions and data captures are done).
    /// </summary>
    /// <remarks>
    /// Fires exactly once per <see cref="BeginCollection"/> call.
    /// </remarks>
    IObservable<AboutFundPageData> Completed { get; }

    /// <summary>
    /// Emits live progress every second during the collection, including step statuses,
    /// elapsed/remaining time, and current slot data.
    /// </summary>
    IObservable<AboutFundCollectionProgress> StateChanged { get; }

    /// <summary>
    /// Emits a snapshot of the current <see cref="AboutFundPageData"/> each time a slot
    /// transitions from <see cref="Domain.ValueObjects.AboutFundFetchStatus.Pending"/>
    /// to <see cref="Domain.ValueObjects.AboutFundFetchStatus.Succeeded"/> or
    /// <see cref="Domain.ValueObjects.AboutFundFetchStatus.Failed"/>.
    /// </summary>
    /// <remarks>
    /// Used by the orchestrator for per-slot persistence in manual collection mode.
    /// Does not fire for repeated updates to an already-resolved slot.
    /// </remarks>
    IObservable<AboutFundPageData> SlotUpdated { get; }

    /// <summary>
    /// Begins collecting data for a new fund page visit using pre-calculated step timings.
    /// Schedules interaction timers at the prescribed absolute times and starts execution.
    /// </summary>
    /// <param name="schedule">
    /// Pre-calculated schedule with step timings, start/stop times, and fund identity.
    /// Built by the orchestrator which owns all scheduling policy.
    /// </param>
    /// <returns>The initial progress snapshot with step statuses and timing data.</returns>
    AboutFundCollectionProgress BeginCollection(AboutFundCollectionSchedule schedule);

    /// <summary>
    /// Routes an intercepted HTTP response to the appropriate data slot
    /// by matching URL patterns. Triggers completion when in the
    /// <see cref="CollectionPhase.Draining"/> phase (all interactions have fired).
    /// </summary>
    /// <param name="request">The intercepted HTTP request/response data.</param>
    void NotifyResponseCaptured(AboutFundInterceptedRequest request);

    /// <summary>
    /// Begins tracking responses for a fund without scheduling any page interactions.
    /// Used for manual collection where the user clicks buttons themselves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Creates an empty <see cref="AboutFundPageData"/> accumulator and accepts
    /// incoming responses via <see cref="NotifyResponseCaptured"/>, but does not
    /// schedule any timers or page interactions. The collection stays open until
    /// a new collection begins or <see cref="CancelCollection"/> is called.
    /// </para>
    /// <para>
    /// If a previous collection is active, it is force-completed and emitted
    /// on <see cref="Completed"/> before the new passive collection starts.
    /// </para>
    /// </remarks>
    /// <param name="orderBookId">The fund's OrderBookId for the page being visited.</param>
    void BeginPassiveCollection(OrderBookId orderBookId);

    /// <summary>
    /// Cancels the active collection, disposing all scheduled interaction timers
    /// and resetting internal state. No-op if no collection is active.
    /// </summary>
    void CancelCollection();
}
