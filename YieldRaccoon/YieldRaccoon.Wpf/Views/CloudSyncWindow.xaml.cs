using MahApps.Metro.Controls;
using NLog;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf.Views;

/// <summary>
/// Interaction logic for CloudSyncWindow.xaml
/// </summary>
public partial class CloudSyncWindow : MetroWindow
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudSyncWindow"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="viewModel">The cloud sync view model.</param>
    public CloudSyncWindow(ILogger logger, CloudSyncWindowViewModel viewModel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        DataContext = viewModel;

        _logger.Debug("CloudSyncWindow initialized");
    }
}
