using System.Windows;
using Autofac;
using NLog;
using YieldRaccoon.Wpf.Views;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for showing the Fund Statistics Export window using Autofac to resolve the window.
/// </summary>
public class FundStatisticsExportWindowService : IFundStatisticsExportWindowService
{
    private readonly ILogger _logger;
    private readonly ILifetimeScope _lifetimeScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundStatisticsExportWindowService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="lifetimeScope">Autofac lifetime scope for resolving the window.</param>
    public FundStatisticsExportWindowService(ILogger logger, ILifetimeScope lifetimeScope)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
    }

    /// <inheritdoc />
    public void ShowFundStatisticsExportWindow()
    {
        _logger.Debug("Showing fund statistics export window");

        try
        {
            var window = _lifetimeScope.Resolve<FundStatisticsExportWindow>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();

            _logger.Debug("Fund statistics export window closed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error showing fund statistics export window");
        }
    }
}
