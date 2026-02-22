using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.Events.AboutFund;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Orchestrates about-fund browsing session lifecycle, navigation workflow, and timer management.
/// </summary>
/// <remarks>
/// <para>
/// This service manages the workflow of navigating through fund detail pages:
/// <list type="bullet">
///   <item>Session lifecycle (start, cancel, complete)</item>
///   <item>Fund navigation sequencing</item>
///   <item>Auto-advance timer management</item>
///   <item>Event publishing to about-fund event store</item>
///   <item>State projection from events</item>
/// </list>
/// </para>
/// <para>
/// The presentation layer should:
/// <list type="bullet">
///   <item>Call command methods (<see cref="StartSessionAsync"/>, <see cref="CancelSession"/>, etc.)</item>
///   <item>Subscribe to observable streams for state and navigation updates</item>
///   <item>Handle <see cref="NavigateToUrl"/> by navigating WebView2</item>
/// </list>
/// </para>
/// </remarks>
public interface IAboutFundOrchestrator : IDisposable
{
    #region Observable Streams

    /// <summary>
    /// Periodically emits detailed session progress via an internal timer.
    /// Includes current fund, elapsed and remaining time, and per-page collection state.
    /// </summary>
    IObservable<AboutFundSessionState> SessionState { get; }

    /// <summary>
    /// Emits domain events about session lifecycle and individual page visits.
    /// Published when state changes (session started/completed/cancelled, navigation started/completed/failed).
    /// </summary>
    IObservable<IAboutFundEvent> Events { get; }

    /// <summary>
    /// Emits a <see cref="Uri"/> when the orchestrator requests navigation to a fund detail page.
    /// </summary>
    /// <remarks>
    /// The presentation layer should handle this by navigating WebView2 to the URL.
    /// This follows the Intent Signal Pattern. Convert to string at the WebView2 boundary.
    /// </remarks>
    IObservable<Uri> NavigateToUrl { get; }

    #endregion

    #region Commands

    /// <summary>
    /// Starts a new browsing session by querying funds with the lowest history counts.
    /// </summary>
    /// <returns>The new session's unique correlation ID.</returns>
    Task<AboutFundSessionId> StartSessionAsync();

    /// <summary>
    /// Cancels the active session with the given reason.
    /// </summary>
    /// <param name="reason">Human-readable reason for cancellation.</param>
    void CancelSession(string reason);

    /// <summary>
    /// Advances to the next fund in the schedule.
    /// </summary>
    void AdvanceToNextFund();

    /// <summary>
    /// Enables or disables automatic advancement through the fund schedule.
    /// </summary>
    /// <param name="enabled">True to enable auto-advance with timer, false to disable.</param>
    void SetAutoAdvance(bool enabled);

    /// <summary>
    /// Loads the fund schedule from the database without starting a session.
    /// </summary>
    /// <returns>The list of funds scheduled for browsing.</returns>
    Task<IReadOnlyList<AboutFundScheduleItem>> LoadScheduleAsync();

    #endregion
}
