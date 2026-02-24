using MahApps.Metro.Controls;
using NLog;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf.Views;

/// <summary>
/// Interaction logic for ExportWindow.xaml
/// </summary>
public partial class ExportWindow : MetroWindow
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportWindow"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="viewModel">The export view model.</param>
    public ExportWindow(ILogger logger, ExportWindowViewModel viewModel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        DataContext = viewModel;

        _logger.Debug("ExportWindow initialized");
    }
}
