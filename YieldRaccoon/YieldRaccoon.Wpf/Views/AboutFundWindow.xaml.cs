using MahApps.Metro.Controls;
using NLog;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf.Views;

/// <summary>
/// Interaction logic for AboutFundWindow.xaml
/// </summary>
public partial class AboutFundWindow : MetroWindow
{
    private readonly ILogger _logger;
    private readonly AboutFundWindowViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundWindow"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="viewModel">The AboutFund window view model.</param>
    public AboutFundWindow(ILogger logger, AboutFundWindowViewModel viewModel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        DataContext = viewModel;

        // Subscribe to close request from ViewModel
        viewModel.CloseRequested += OnCloseRequested;

        _logger.Debug("AboutFundWindow initialized");
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        _logger.Debug("Close requested");
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Cleanup subscriptions
        _viewModel.CloseRequested -= OnCloseRequested;
        _viewModel.Dispose();

        _logger.Debug("AboutFundWindow closed and disposed");
    }
}
