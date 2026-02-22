using MahApps.Metro.Controls;
using Microsoft.Web.WebView2.Core;
using NLog;
using YieldRaccoon.Wpf.Services;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf.Views;

/// <summary>
/// AboutFund browser window — handles WebView2 initialization and interceptor wiring.
/// </summary>
/// <remarks>
/// <para>
/// Code-behind is intentionally minimal (UI plumbing only):
/// <list type="bullet">
///   <item>Initializes <see cref="IAboutFundResponseInterceptor"/> when CoreWebView2 is ready</item>
///   <item>Captures/clears privacy screenshot on mode toggle (HWND airspace workaround)</item>
///   <item>Disposes the view-owned interceptor on window close</item>
/// </list>
/// </para>
/// <para>
/// ViewModel lifecycle is managed via <see cref="DevExpress.Mvvm.UI.CurrentWindowService.ClosingCommand"/>
/// — the ViewModel disposes itself when the window closes, no code-behind involvement.
/// </para>
/// </remarks>
public partial class AboutFundWindow : MetroWindow
{
    private readonly ILogger _logger;
    private readonly AboutFundWindowViewModel _viewModel;
    private readonly IAboutFundResponseInterceptor _interceptor;
    private readonly WebView2AboutFundPageInteractor _pageInteractor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundWindow"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="viewModel">The AboutFund window view model.</param>
    /// <param name="interceptor">The response interceptor for capturing WebView2 network traffic.</param>
    /// <param name="pageInteractor">The page interactor for post-navigation element clicks (singleton, shared with orchestrator).</param>
    public AboutFundWindow(
        ILogger logger,
        AboutFundWindowViewModel viewModel,
        IAboutFundResponseInterceptor interceptor,
        WebView2AboutFundPageInteractor pageInteractor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        _pageInteractor = pageInteractor ?? throw new ArgumentNullException(nameof(pageInteractor));

        InitializeComponent();

        DataContext = viewModel;

        viewModel.PrivacyModeChanged += OnPrivacyModeChanged;

        // Initialize interceptor when WebView2 CoreWebView2 is ready
        Browser.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;

        _logger.Debug("AboutFundWindow initialized");
    }

    private void OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _interceptor.Initialize(Browser);
            _pageInteractor.Initialize(Browser);
            _logger.Debug("Response interceptor and page interactor initialized");
        }
        else
        {
            _logger.Error(e.InitializationException, "CoreWebView2 initialization failed, interceptor not started");
        }
    }

    /// <summary>
    /// Handles privacy mode toggle — captures screenshot before hiding WebView2 (HWND airspace).
    /// </summary>
    private async void OnPrivacyModeChanged(object? sender, EventArgs e)
    {
        if (_viewModel.IsPrivacyMode)
        {
            if (Browser.CoreWebView2 == null)
            {
                _logger.Warn("Cannot capture privacy screenshot: CoreWebView2 not initialized");
                return;
            }

            // Capture screenshot BEFORE hiding browser (HWND must be visible to capture)
            _viewModel.PrivacyScreenshot = await PrivacyFilterService.CaptureAndFilterAsync(
                Browser.CoreWebView2, Dispatcher);

            // Now hide browser so WPF overlay becomes visible
            Browser.Visibility = System.Windows.Visibility.Collapsed;
        }
        else
        {
            Browser.Visibility = System.Windows.Visibility.Visible;
            _viewModel.PrivacyScreenshot = null;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _viewModel.PrivacyModeChanged -= OnPrivacyModeChanged;
        _interceptor.Dispose();

        _logger.Debug("AboutFundWindow closed");
    }
}