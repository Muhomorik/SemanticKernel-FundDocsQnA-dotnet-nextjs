using MahApps.Metro.Controls;
using NLog;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : MetroWindow
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="viewModel">The settings view model.</param>
    public SettingsWindow(ILogger logger, SettingsWindowViewModel viewModel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        DataContext = viewModel;

        // Subscribe to close request from ViewModel
        viewModel.CloseRequested += OnCloseRequested;

        _logger.Debug("SettingsWindow initialized");
    }

    private void OnCloseRequested(object? sender, bool dialogResult)
    {
        _logger.Debug($"Close requested with result: {dialogResult}");
        DialogResult = dialogResult;
        Close();
    }
}
