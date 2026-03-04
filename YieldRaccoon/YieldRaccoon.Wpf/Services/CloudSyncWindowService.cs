using Autofac;
using NLog;
using YieldRaccoon.Wpf.Views;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for showing the Cloud Sync window using Autofac to resolve the window.
/// </summary>
public class CloudSyncWindowService : ICloudSyncWindowService
{
    private readonly ILogger _logger;
    private readonly ILifetimeScope _lifetimeScope;

    public CloudSyncWindowService(ILogger logger, ILifetimeScope lifetimeScope)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
    }

    /// <inheritdoc />
    public void ShowCloudSyncWindow()
    {
        _logger.Debug("Showing cloud sync window");

        try
        {
            var window = _lifetimeScope.Resolve<CloudSyncWindow>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();

            _logger.Debug("Cloud sync window closed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error showing cloud sync window");
        }
    }
}
