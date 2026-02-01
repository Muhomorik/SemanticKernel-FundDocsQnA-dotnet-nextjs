using System.Windows;
using Autofac;
using NLog;
using YieldRaccoon.Wpf.Views;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for showing the settings dialog using Autofac to resolve the window.
/// </summary>
public class SettingsDialogService : ISettingsDialogService
{
    private readonly ILogger _logger;
    private readonly ILifetimeScope _lifetimeScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsDialogService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="lifetimeScope">Autofac lifetime scope for resolving the settings window.</param>
    public SettingsDialogService(ILogger logger, ILifetimeScope lifetimeScope)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
    }

    /// <inheritdoc />
    public bool ShowSettingsDialog()
    {
        _logger.Debug("Showing settings dialog");

        try
        {
            var settingsWindow = _lifetimeScope.Resolve<SettingsWindow>();
            settingsWindow.Owner = System.Windows.Application.Current.MainWindow;

            var result = settingsWindow.ShowDialog() == true;

            _logger.Debug($"Settings dialog closed with result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error showing settings dialog");
            return false;
        }
    }
}
