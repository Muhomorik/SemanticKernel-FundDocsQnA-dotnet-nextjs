namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Manages a per-user Windows scheduled task that launches YieldRaccoon daily at a user-chosen time.
/// Implementations write to the <c>\YieldRaccoon\</c> folder in Task Scheduler so creation/deletion
/// works without admin elevation under normal Windows configurations.
/// </summary>
public interface IAutoStartSchedulerService
{
    /// <summary>
    /// Creates or updates the daily auto-start scheduled task.
    /// </summary>
    /// <param name="timeOfDay">Local time of day for the daily trigger. Only hour/minute are used.</param>
    /// <param name="passAutoListFlag">
    /// When true, the scheduled action launches the exe with <c>--auto-list</c> to start the fund list
    /// crawl automatically. When false, the exe is launched without any CLI arguments.
    /// </param>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when Windows denies task creation (Group Policy, hardened image, etc.).
    /// The caller should prompt the user to restart as administrator.
    /// </exception>
    void EnableDaily(TimeSpan timeOfDay, bool passAutoListFlag);

    /// <summary>
    /// Removes the auto-start scheduled task if it exists. Silent no-op when the task is absent.
    /// </summary>
    void Disable();

    /// <summary>
    /// Returns true if the auto-start scheduled task currently exists in Task Scheduler.
    /// Used to reconcile persisted settings with the actual task state on startup.
    /// </summary>
    bool IsEnabled();
}
