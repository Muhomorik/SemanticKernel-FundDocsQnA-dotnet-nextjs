using YieldRaccoon.Application.DTOs;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Orchestrates crawl session lifecycle, batch loading workflow, and timer management.
/// </summary>
/// <remarks>
/// <para>
/// This service owns all session-related business logic that was previously in the ViewModel:
/// <list type="bullet">
///   <item>Session lifecycle (start, cancel, complete)</item>
///   <item>Batch workflow orchestration</item>
///   <item>Countdown timer management</item>
///   <item>Domain event publishing to event store</item>
///   <item>State projection from events</item>
/// </list>
/// </para>
/// <para>
/// The presentation layer should:
/// <list type="bullet">
///   <item>Call command methods (<see cref="StartSession"/>, <see cref="CancelSession"/>, etc.)</item>
///   <item>Subscribe to observable streams for state updates</item>
///   <item>Handle <see cref="LoadBatchRequested"/> by clicking "Visa fler" button</item>
/// </list>
/// </para>
/// <para>
/// This follows the Intent Signal Pattern: the orchestrator emits intents (load batch),
/// and the presentation layer decides how to fulfill them (click DOM element).
/// </para>
/// </remarks>
public interface ICrawlSessionOrchestrator : IDisposable
{
    #region Commands

    /// <summary>
    /// Starts a new crawl session with pre-scheduled batch timings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Delegates to <see cref="ICrawlSessionScheduler"/> to pre-calculate all batch times,
    /// then immediately requests loading the first batch.
    /// </para>
    /// </remarks>
    /// <returns>The new session's unique correlation ID.</returns>
    CrawlSessionId StartSession();

    /// <summary>
    /// Cancels the active session with the given reason.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stops any active countdown timer and appends <c>CrawlSessionCancelled</c> event.
    /// </para>
    /// </remarks>
    /// <param name="reason">Human-readable reason for cancellation.</param>
    void CancelSession(string reason);

    /// <summary>
    /// Notifies the orchestrator that a batch of funds was successfully loaded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be called by the presentation layer after receiving fund data
    /// from the network intercept. The orchestrator will:
    /// <list type="bullet">
    ///   <item>Persist the fund data via <see cref="IFundIngestionService"/></item>
    ///   <item>Append <c>BatchLoadCompleted</c> event</item>
    ///   <item>Start countdown for next batch if <paramref name="hasMore"/> is true</item>
    ///   <item>Complete the session if no more funds are available</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="funds">The fund data to persist to the database.</param>
    /// <param name="totalFundsLoaded">Total funds loaded so far across all batches.</param>
    /// <param name="hasMore">Whether more funds are available to load.</param>
    Task NotifyBatchLoadedAsync(IReadOnlyCollection<FundDataDto> funds, int totalFundsLoaded, bool hasMore);

    /// <summary>
    /// Ingests fund data to the database without requiring an active session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this method when funds are received outside of an active crawl session,
    /// such as during initial page load or manual batch loading.
    /// </para>
    /// <para>
    /// This method only persists data; it does not publish session events or
    /// manage batch scheduling.
    /// </para>
    /// </remarks>
    /// <param name="funds">The fund data to persist to the database.</param>
    /// <returns>The number of successfully ingested funds.</returns>
    Task<int> IngestFundsAsync(IReadOnlyCollection<FundDataDto> funds);

    #endregion

    #region Observable Streams

    /// <summary>
    /// Emits the current session state whenever it changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this stream to update UI properties like:
    /// <c>IsSessionActive</c>, <c>CurrentBatchNumber</c>, <c>EstimatedTimeRemaining</c>, etc.
    /// </para>
    /// <para>
    /// Emits <see cref="CrawlSessionState.Inactive"/> when no session is active.
    /// </para>
    /// </remarks>
    IObservable<CrawlSessionState> SessionState { get; }

    /// <summary>
    /// Emits the scheduled batch list whenever it changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this stream to populate the scheduled events list UI.
    /// Each item shows batch number, scheduled time, and current status.
    /// </para>
    /// </remarks>
    IObservable<IReadOnlyList<ScheduledBatchItem>> ScheduledBatches { get; }

    /// <summary>
    /// Emits countdown updates every second during batch delay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this stream to update the countdown display.
    /// Ticks are emitted each second until the delay completes.
    /// </para>
    /// </remarks>
    IObservable<CountdownTick> CountdownTick { get; }

    /// <summary>
    /// Emits when the orchestrator requests loading the next batch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The presentation layer should handle this by clicking the "Visa fler" button.
    /// This follows the Intent Signal Pattern: orchestrator requests action,
    /// presentation layer decides how to fulfill it.
    /// </para>
    /// </remarks>
    IObservable<BatchNumber> LoadBatchRequested { get; }

    /// <summary>
    /// Emits when the session completes successfully (all funds loaded).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this stream for completion notifications or statistics display.
    /// </para>
    /// </remarks>
    IObservable<SessionCompletedInfo> SessionCompleted { get; }

    #endregion
}
