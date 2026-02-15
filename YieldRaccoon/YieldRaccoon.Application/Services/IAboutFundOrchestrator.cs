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
    /// Notifies the orchestrator that a fund page has finished loading.
    /// </summary>
    void NotifyNavigationCompleted();

    /// <summary>
    /// Advances to the next fund in the schedule (marks current as completed).
    /// </summary>
    void AdvanceToNextFund();

    /// <summary>
    /// Enables or disables automatic advancement through the fund schedule.
    /// </summary>
    /// <param name="enabled">True to enable auto-advance with timer, false to disable.</param>
    void SetAutoAdvance(bool enabled);

    /// <summary>
    /// Loads the fund schedule from database without starting a session.
    /// </summary>
    /// <returns>The list of funds scheduled for browsing.</returns>
    Task<IReadOnlyList<AboutFundScheduleItem>> LoadScheduleAsync();

    /// <summary>
    /// Notifies the orchestrator that a network response was captured from the WebView2 browser.
    /// </summary>
    /// <param name="request">The intercepted HTTP request/response data.</param>
    void NotifyResponseCaptured(AboutFundInterceptedRequest request);

    #endregion

    #region Observable Streams

    /// <summary>
    /// Emits the current session state whenever it changes.
    /// </summary>
    IObservable<AboutFundSessionState> SessionState { get; }

    /// <summary>
    /// Emits each about-fund event as it is published.
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

    /// <summary>
    /// Emits countdown updates every second during auto-advance delay.
    /// Each value is the number of seconds remaining until the next fund loads.
    /// </summary>
    IObservable<int> CountdownTick { get; }

    #endregion
}