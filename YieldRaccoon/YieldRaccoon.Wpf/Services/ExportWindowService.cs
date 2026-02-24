using System.Windows;
using Autofac;
using NLog;
using YieldRaccoon.Wpf.Views;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for showing the Export window using Autofac to resolve the window.
/// </summary>
public class ExportWindowService : IExportWindowService
{
    private readonly ILogger _logger;
    private readonly ILifetimeScope _lifetimeScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportWindowService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="lifetimeScope">Autofac lifetime scope for resolving the export window.</param>
    public ExportWindowService(ILogger logger, ILifetimeScope lifetimeScope)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
    }

    /// <inheritdoc />
    public void ShowExportWindow()
    {
        _logger.Debug("Showing export window");

        try
        {
            var exportWindow = _lifetimeScope.Resolve<ExportWindow>();
            exportWindow.Owner = System.Windows.Application.Current.MainWindow;
            exportWindow.ShowDialog();

            _logger.Debug("Export window closed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error showing export window");
        }
    }
}
