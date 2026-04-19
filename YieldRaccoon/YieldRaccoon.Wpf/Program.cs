using System;
using System.Collections.ObjectModel;
using Microsoft.VisualBasic.ApplicationServices;

namespace YieldRaccoon.Wpf;

/// <summary>
/// Single-instance entry point. Uses <see cref="WindowsFormsApplicationBase"/> to enforce
/// one running process per user and to forward command-line args from any second launch
/// (e.g. the Thursday 22:00 Windows scheduled task) to the already-running first instance.
/// </summary>
/// <remarks>
/// Pattern recommended by Microsoft Learn for WPF single-instance apps. The WinForms
/// message loop is not started; we only use the base class for its single-instance and
/// command-line-forwarding infrastructure.
/// </remarks>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var manager = new SingleInstanceManager();
        manager.Run(args);
    }
}

/// <summary>
/// VB Application-model wrapper that hosts the WPF <see cref="App"/> and routes forwarded
/// command-line invocations to the running instance.
/// </summary>
internal sealed class SingleInstanceManager : WindowsFormsApplicationBase
{
    private App? _app;

    public SingleInstanceManager()
    {
        IsSingleInstance = true;
    }

    /// <summary>
    /// Creates and runs the WPF <see cref="App"/>. Returns <c>false</c> so the VB
    /// WinForms message loop does NOT run (we use the WPF dispatcher instead).
    /// </summary>
    protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
    {
        _app = new App();
        _app.InitializeComponent();
        _app.Run();
        return false;
    }

    /// <summary>
    /// Fires on the running (first) instance when a second launch is attempted.
    /// The second process's command line is forwarded via <see cref="StartupNextInstanceEventArgs.CommandLine"/>.
    /// We route <c>--auto-weekly-stats</c> to <see cref="App.HandleAutoWeeklyStatsTrigger"/>.
    /// </summary>
    protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
    {
        base.OnStartupNextInstance(e);

        e.BringToForeground = true;

        if (_app is null)
            return;

        if (ContainsAutoWeeklyStatsFlag(e.CommandLine))
            _app.HandleAutoWeeklyStatsTrigger();
    }

    private static bool ContainsAutoWeeklyStatsFlag(ReadOnlyCollection<string> commandLine)
    {
        for (var i = 0; i < commandLine.Count; i++)
        {
            if (string.Equals(commandLine[i], "--auto-weekly-stats", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
