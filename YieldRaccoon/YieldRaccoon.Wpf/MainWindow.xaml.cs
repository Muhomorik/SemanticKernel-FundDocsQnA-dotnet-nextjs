using MahApps.Metro.Controls;
using Microsoft.Web.WebView2.Core;
using NLog;
using YieldRaccoon.Wpf.Services;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf;

/// <summary>
/// Main window code-behind — handles WebView2 initialization, interceptor/interactor wiring,
/// and privacy screenshot capture.
/// </summary>
/// <remarks>
/// <para>
/// Code-behind is intentionally minimal (UI plumbing only):
/// <list type="bullet">
///   <item>Initializes <see cref="IFundListResponseInterceptor"/> and <see cref="IFundListPageInteractor"/> when CoreWebView2 is ready</item>
///   <item>Delegates page interactions (pagination clicks, scrolling, reload) to <see cref="IFundListPageInteractor"/></item>
///   <item>Captures/clears privacy screenshot on mode toggle (HWND airspace workaround)</item>
///   <item>Disposes view-owned services on window close</item>
/// </list>
/// </para>
/// </remarks>
public partial class MainWindow : MetroWindow
{
    private readonly ILogger _logger;
    private readonly MainWindowViewModel _viewModel;
    private readonly IFundListResponseInterceptor _interceptor;
    private readonly IFundListPageInteractor _pageInteractor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="viewModel">The view model for the main window.</param>
    /// <param name="interceptor">The response interceptor for capturing fund list network traffic.</param>
    /// <param name="pageInteractor">The page interactor for pagination clicks, scrolling, and reloading.</param>
    public MainWindow(
        ILogger logger,
        MainWindowViewModel viewModel,
        IFundListResponseInterceptor interceptor,
        IFundListPageInteractor pageInteractor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        _pageInteractor = pageInteractor ?? throw new ArgumentNullException(nameof(pageInteractor));

        _logger.Debug("MainWindow constructor called");
        InitializeComponent();
        DataContext = viewModel;

        // Subscribe to ViewModel events
        _viewModel.BrowserReloadRequested += OnBrowserReloadRequested;
        _viewModel.RequestLoadMoreFunds += OnRequestLoadMoreFunds;
        _viewModel.PrivacyModeChanged += OnPrivacyModeChanged;
        _viewModel.BrowserScrollToEndRequested += OnBrowserScrollToEndRequested;

        // Wire up WebView2 initialization and events
        InitializeAsync();
        _logger.Info("MainWindow initialized successfully");
    }

    /// <summary>
    /// Initializes WebView2, wires up navigation events, and initializes services.
    /// </summary>
    private async void InitializeAsync()
    {
        try
        {
            _logger.Debug("Starting WebView2 initialization");
            await Browser.EnsureCoreWebView2Async();

            // Wire up navigation events
            Browser.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            Browser.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            // Initialize services with the ready WebView2
            _interceptor.Initialize(Browser);
            _interceptor.FundDataIntercepted += OnFundDataIntercepted;

            _pageInteractor.Initialize(Browser);

            // Notify ViewModel that WebView2 is ready
            _viewModel.OnWebView2Initialized();

            _logger.Info("WebView2 initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize WebView2");
            throw;
        }
    }

    /// <summary>
    /// Handles the navigation starting event.
    /// </summary>
    private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        _logger.Debug("Navigation starting: {0}", e.Uri);
        Dispatcher.Invoke(() => _viewModel.OnBrowserLoadingStateChanged(true));
    }

    /// <summary>
    /// Handles the navigation completed event.
    /// </summary>
    private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _logger.Debug("Navigation completed. Success: {0}", e.IsSuccess);
        if (!e.IsSuccess) _logger.Warn("Navigation failed with status: {0}", e.WebErrorStatus);
        Dispatcher.Invoke(() => _viewModel.OnBrowserLoadingStateChanged(false));

        // Update screenshot if privacy mode is active
        if (_viewModel.IsPrivacyMode) await CapturePrivacyScreenshotOffScreenAsync();
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

    /// <summary>
    /// Captures privacy screenshot with browser temporarily moved off-screen.
    /// </summary>
    /// <remarks>
    /// WebView2 is an HWND control — must be visible to capture, but we move it off-screen
    /// so user doesn't see sensitive content during the brief capture window.
    /// </remarks>
    private async Task CapturePrivacyScreenshotOffScreenAsync()
    {
        if (Browser.CoreWebView2 == null)
            return;

        try
        {
            var originalTransform = Browser.RenderTransform;
            Browser.RenderTransform = new System.Windows.Media.TranslateTransform(-10000, 0);

            Browser.Visibility = System.Windows.Visibility.Visible;
            await Task.Delay(50);

            _viewModel.PrivacyScreenshot = await PrivacyFilterService.CaptureAndFilterAsync(
                Browser.CoreWebView2, Dispatcher);

            Browser.Visibility = System.Windows.Visibility.Collapsed;
            Browser.RenderTransform = originalTransform;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to capture off-screen privacy screenshot");
            Browser.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Handles browser reload request from ViewModel.
    /// </summary>
    private void OnBrowserReloadRequested(object? sender, EventArgs e)
    {
        _logger.Debug("Browser reload requested from ViewModel");
        _pageInteractor.Reload();
    }

    /// <summary>
    /// Handles intercepted fund data from network responses.
    /// </summary>
    private void OnFundDataIntercepted(object? sender, Models.FundListDataInterceptedEventArgs e)
    {
        _logger.Info("Fund data intercepted: {0} funds from {1}",
            e.FundData?.Funds?.Count ?? 0, e.SourceUri);

        // Forward to ViewModel on UI thread
        Dispatcher.Invoke(() => { _viewModel.OnFundDataReceived(e.FundData); });
    }

    /// <summary>
    /// Handles the request to load more funds by clicking the "Visa fler" button.
    /// </summary>
    private async void OnRequestLoadMoreFunds(object? sender, EventArgs e)
    {
        _logger.Info("Request to load more funds received");

        try
        {
            var buttonFound = await _pageInteractor.ClickLoadMoreButtonAsync();

            // If button was not found, stop pagination
            if (!buttonFound)
            {
                _logger.Warn("'Visa fler' button not found on page. Stopping pagination.");
                if (_viewModel.IsPaginationInProgress) _viewModel.IsPaginationInProgress = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while trying to load more funds");
            _viewModel.IsPaginationInProgress = false;
        }
    }

    /// <summary>
    /// Handles the browser scroll to end request.
    /// Updates privacy screenshot after content has rendered.
    /// </summary>
    private async void OnBrowserScrollToEndRequested(object? sender, EventArgs e)
    {
        await _pageInteractor.ScrollToEndAsync();

        // Update privacy screenshot after scroll and DOM render
        if (_viewModel.IsPrivacyMode)
        {
            await Task.Delay(500);
            await CapturePrivacyScreenshotOffScreenAsync();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _viewModel.BrowserReloadRequested -= OnBrowserReloadRequested;
        _viewModel.RequestLoadMoreFunds -= OnRequestLoadMoreFunds;
        _viewModel.PrivacyModeChanged -= OnPrivacyModeChanged;
        _viewModel.BrowserScrollToEndRequested -= OnBrowserScrollToEndRequested;

        _interceptor.Dispose();
        _pageInteractor.Dispose();

        _logger.Debug("MainWindow closed");
    }
}
