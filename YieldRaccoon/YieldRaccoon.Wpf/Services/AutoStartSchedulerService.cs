using System.IO;
using System.Linq;
using Microsoft.Win32.TaskScheduler;
using NLog;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Windows Task Scheduler backed implementation of <see cref="IAutoStartSchedulerService"/>.
/// Uses the dahall/TaskScheduler NuGet wrapper over the native Task Scheduler 2.0 COM API.
/// </summary>
/// <remarks>
/// Tasks are registered under <c>\YieldRaccoon\YieldRaccoon-AutoStart</c> with
/// <see cref="TaskLogonType.InteractiveToken"/> + <see cref="TaskRunLevel.LUA"/>, which lets a
/// standard user create, update, and delete their own tasks without UAC elevation on normal
/// Windows installs. Access-denied errors (Group Policy, hardened images) surface as
/// <see cref="UnauthorizedAccessException"/> so the ViewModel can prompt for elevation.
/// </remarks>
public class AutoStartSchedulerService : IAutoStartSchedulerService
{
    private const string FolderName = "YieldRaccoon";
    private const string TaskName = "YieldRaccoon-AutoStart";
    private const string FullTaskPath = @"\YieldRaccoon\YieldRaccoon-AutoStart";

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoStartSchedulerService"/> class.
    /// </summary>
    /// <param name="logger">NLog logger (injected via NLogModule).</param>
    public AutoStartSchedulerService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void EnableDaily(TimeSpan timeOfDay, bool passAutoListFlag)
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to resolve current process path for scheduled task.");
        var workingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty;
        var arguments = passAutoListFlag ? "--auto-list" : string.Empty;

        using var taskService = new TaskService();

        // Ensure the user subfolder exists. SubFolders iteration returns existing subfolders only;
        // CreateFolder is idempotent enough when guarded by a null check.
        var folder = taskService.RootFolder.SubFolders
                         .Cast<TaskFolder>()
                         .FirstOrDefault(f => string.Equals(f.Name, FolderName, StringComparison.OrdinalIgnoreCase))
                     ?? taskService.RootFolder.CreateFolder(FolderName);

        var definition = taskService.NewTask();
        definition.RegistrationInfo.Description = "YieldRaccoon daily auto-start for fund list crawling.";
        definition.RegistrationInfo.Author = Environment.UserName;

        definition.Triggers.Add(new DailyTrigger
        {
            StartBoundary = DateTime.Today.Add(timeOfDay),
            DaysInterval = 1
        });

        definition.Actions.Add(new ExecAction(exePath, arguments, workingDirectory));

        definition.Principal.LogonType = TaskLogonType.InteractiveToken;
        definition.Principal.RunLevel = TaskRunLevel.LUA;

        // Don't block the daily trigger on battery state — this is a background fund crawl,
        // not a heavy workload, and a laptop on battery is a valid host.
        definition.Settings.DisallowStartIfOnBatteries = false;
        definition.Settings.StopIfGoingOnBatteries = false;
        // Catch up if the PC was off/asleep at the scheduled time (up to ~72 hours).
        definition.Settings.StartWhenAvailable = true;
        // Wake the PC from sleep to run the scheduled crawl. Requires the Windows power plan
        // to allow wake timers (Power Options → Sleep → Allow wake timers = Enabled).
        definition.Settings.WakeToRun = true;

        folder.RegisterTaskDefinition(
            TaskName,
            definition,
            TaskCreation.CreateOrUpdate,
            userId: null,
            password: null,
            logonType: TaskLogonType.InteractiveToken);

        _logger.Info(
            "Registered auto-start scheduled task at {0:hh\\:mm} (autoList={1}, exe={2})",
            timeOfDay, passAutoListFlag, exePath);
    }

    /// <inheritdoc />
    public void Disable()
    {
        using var taskService = new TaskService();

        var folder = taskService.RootFolder.SubFolders
            .Cast<TaskFolder>()
            .FirstOrDefault(f => string.Equals(f.Name, FolderName, StringComparison.OrdinalIgnoreCase));

        if (folder is null)
        {
            _logger.Debug("Auto-start task folder does not exist, nothing to disable");
            return;
        }

        folder.DeleteTask(TaskName, exceptionOnNotExists: false);
        _logger.Info("Removed auto-start scheduled task");
    }

    /// <inheritdoc />
    public bool IsEnabled()
    {
        using var taskService = new TaskService();
        return taskService.GetTask(FullTaskPath) is not null;
    }
}
