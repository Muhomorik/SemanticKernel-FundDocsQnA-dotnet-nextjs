using CommandLine;

namespace YieldRaccoon.Wpf.Configuration;

/// <summary>
/// Command-line arguments controlling auto-start behavior for fund crawling sessions.
/// Parsed via CommandLineParser at application startup and registered as a singleton in the DI container.
/// </summary>
/// <remarks>
/// <para>
/// Usage:
/// <code>
/// YieldRaccoon.Wpf.exe --auto-list --auto-overview 50
/// </code>
/// </para>
/// <para>
/// This class lives in the Presentation layer. The Application layer receives primitive parameters
/// (e.g., <c>int? limit</c>) without knowing they originated from CLI arguments.
/// </para>
/// <para>
/// <b>Cold-start scheduling:</b> when any auto-start flag is set, the first crawler call
/// (both the fund list crawl and the AboutFund overview) is deferred by
/// <see cref="ColdStartDelay"/> measured from application launch. This avoids colliding
/// with EF Core model-building and SQLite warm-up on the first DB call. Subsequent
/// crawler calls run at normal cadence. See <see cref="GetColdStartRemaining(TimeSpan)"/>.
/// </para>
/// </remarks>
public class AutoStartOptions
{
    /// <summary>
    /// Cold-start buffer applied to the first crawler call in auto-started sessions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When launched with <c>--auto-list</c> or <c>--auto-overview</c>, the first
    /// crawler call is scheduled this long after application launch (not after the
    /// trigger point fires). The delay gives EF Core / SQLite enough time to complete
    /// model building, connection pooling, and any initial queries, so the first
    /// orchestrator step doesn't stall or fail on a cold database.
    /// </para>
    /// <para>
    /// Only the <i>first</i> call is delayed; every subsequent step inside the same
    /// session runs without this buffer. Manual (interactive) launches are unaffected —
    /// the delay is only applied where an auto-start flag is consumed.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan ColdStartDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// UTC timestamp captured when this options instance is constructed. Because the
    /// instance is built during <c>App.OnStartup</c> and registered as a singleton, this
    /// is effectively the application launch time and is used as the reference point
    /// for <see cref="GetColdStartRemaining(TimeSpan)"/>.
    /// </summary>
    public DateTime LaunchedAtUtc { get; } = DateTime.UtcNow;

    /// <summary>
    /// Auto-start the fund list crawl session in the Main Window when WebView2 is ready.
    /// </summary>
    [Option("auto-list", Required = false, HelpText = "Auto-start fund list crawl session.")]
    public bool AutoList { get; set; }

    /// <summary>
    /// Auto-open the AboutFund window and start an overview session with N funds.
    /// When set, the AboutFund window opens automatically and begins browsing.
    /// </summary>
    [Option("auto-overview", Required = false, Default = null,
        HelpText = "Auto-open AboutFund and start overview with N funds.")]
    public int? AutoOverviewFundCount { get; set; }

    /// <summary>
    /// When true, the application opens the Settings window immediately after the main window shows.
    /// This flag is only passed by the app itself when it restarts as administrator to retry a
    /// scheduled-task operation that was blocked by UAC. It lets the user click Save again in the
    /// elevated instance without having to navigate back to Settings manually. The flag has no
    /// effect on regular interactive launches and is not intended for manual use.
    /// </summary>
    [Option("elevated-settings", Required = false,
        HelpText = "Internal: open Settings on startup after a UAC-elevated restart.")]
    public bool OpenSettingsOnStartup { get; set; }

    /// <summary>
    /// Gets whether the AboutFund auto-overview mode is active.
    /// </summary>
    public bool AutoOverview => AutoOverviewFundCount.HasValue;

    /// <summary>
    /// Gets the number of funds to include in the AboutFund overview schedule.
    /// Falls back to 80 when not specified (matches the orchestrator's default ScheduleLimit).
    /// </summary>
    public int OverviewFundCount => AutoOverviewFundCount ?? 80;

    /// <summary>
    /// Gets whether any auto-start mode is active. Drives UI badge visibility.
    /// </summary>
    public bool IsAnyAutoModeActive => AutoList || AutoOverview;

    /// <summary>
    /// Default instance with no auto-start (normal interactive launch).
    /// Used for design-time constructors and as a fallback when CLI parsing fails.
    /// </summary>
    public static AutoStartOptions None => new();

    /// <summary>
    /// Computes how much of the cold-start window is still ahead of us, based on
    /// <see cref="LaunchedAtUtc"/>. Callers should await this duration before making
    /// the first crawler / DB call in an auto-started session.
    /// </summary>
    /// <param name="coldStartWindow">
    /// Total cold-start buffer to apply. Callers typically pass <see cref="ColdStartDelay"/>.
    /// </param>
    /// <returns>
    /// The remaining time until the cold-start window elapses, clamped to
    /// <see cref="TimeSpan.Zero"/> if the window has already passed (e.g., the user
    /// spent more than a minute clicking through Settings before auto-start fired).
    /// </returns>
    public TimeSpan GetColdStartRemaining(TimeSpan coldStartWindow)
    {
        var elapsed = DateTime.UtcNow - LaunchedAtUtc;
        var remaining = coldStartWindow - elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
