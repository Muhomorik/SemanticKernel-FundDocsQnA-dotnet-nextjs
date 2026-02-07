using Autofac;
using NLog;
using YieldRaccoon.Wpf.Views;

namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for showing the AboutFund browser window using Autofac to resolve the window.
/// Manages a single instance of the window (non-modal).
/// </summary>
public class AboutFundWindowService : IAboutFundWindowService
{
    private readonly ILogger _logger;
    private readonly ILifetimeScope _lifetimeScope;
    private AboutFundWindow? _aboutFundWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundWindowService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="lifetimeScope">Autofac lifetime scope for resolving the window.</param>
    public AboutFundWindowService(ILogger logger, ILifetimeScope lifetimeScope)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
    }

    /// <inheritdoc />
    public bool IsAboutFundWindowOpen => _aboutFundWindow is { IsVisible: true };

    /// <inheritdoc />
    public void ShowAboutFundWindow()
    {
        _logger.Debug("ShowAboutFundWindow called, IsOpen: {0}", IsAboutFundWindowOpen);

        try
        {
            if (_aboutFundWindow is { IsVisible: true })
            {
                // Window already open - bring to focus
                _aboutFundWindow.Activate();
                _aboutFundWindow.Focus();
                _logger.Debug("AboutFund window activated (already open)");
                return;
            }

            // Create new window instance
            _aboutFundWindow = _lifetimeScope.Resolve<AboutFundWindow>();

            // Subscribe to closed event for cleanup
            _aboutFundWindow.Closed += OnAboutFundWindowClosed;

            // Non-modal: Use Show() instead of ShowDialog()
            _aboutFundWindow.Show();

            _logger.Info("AboutFund window opened");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error showing AboutFund window");
        }
    }

    private void OnAboutFundWindowClosed(object? sender, EventArgs e)
    {
        _logger.Debug("AboutFund window closed");

        if (_aboutFundWindow != null)
        {
            _aboutFundWindow.Closed -= OnAboutFundWindowClosed;
            _aboutFundWindow = null;
        }
    }
}
