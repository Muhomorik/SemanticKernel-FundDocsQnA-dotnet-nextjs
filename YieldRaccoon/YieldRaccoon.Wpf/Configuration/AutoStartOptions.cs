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
/// </remarks>
public class AutoStartOptions
{
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
}
