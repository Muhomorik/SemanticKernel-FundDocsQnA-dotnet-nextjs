using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;
using MahApps.Metro.Controls;
using Microsoft.Web.WebView2.Core;
using NLog;
using YieldRaccoon.Wpf.Services;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly ILogger _logger;
    private readonly MainWindowViewModel _viewModel;
    private WebView2ResponseInterceptor? _responseInterceptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="viewModel">The view model for the main window.</param>
    public MainWindow(ILogger logger, MainWindowViewModel viewModel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _logger.Debug("MainWindow constructor called");
        InitializeComponent();
        DataContext = viewModel;

        // Subscribe to ViewModel events
        _viewModel.BrowserReloadRequested += OnBrowserReloadRequested;
        _viewModel.RequestLoadMoreFunds += OnRequestLoadMoreFunds;
        _viewModel.StreamingModeChanged += OnStreamingModeChanged;
        _viewModel.BrowserScrollToEndRequested += OnBrowserScrollToEndRequested;

        // Wire up WebView2 initialization and events
        InitializeAsync();
        _logger.Info("MainWindow initialized successfully");
    }

    /// <summary>
    /// Initializes WebView2 and wires up events.
    /// </summary>
    private async void InitializeAsync()
    {
        try
        {
            _logger.Debug("Starting WebView2 initialization");
            // Ensure WebView2 is initialized
            await Browser.EnsureCoreWebView2Async();

            // Wire up navigation events
            Browser.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            Browser.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            // Initialize response interceptor
            _responseInterceptor = new WebView2ResponseInterceptor(Browser, _logger);
            _responseInterceptor.FundDataIntercepted += OnFundDataIntercepted;
            _responseInterceptor.Initialize();

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
        _logger.Debug($"Navigation starting: {e.Uri}");
        // Update ViewModel on UI thread
        Dispatcher.Invoke(() => _viewModel.OnBrowserLoadingStateChanged(true));
    }

    /// <summary>
    /// Handles the navigation completed event.
    /// </summary>
    private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _logger.Debug($"Navigation completed. Success: {e.IsSuccess}");
        if (!e.IsSuccess) _logger.Warn($"Navigation failed with status: {e.WebErrorStatus}");
        // Update ViewModel on UI thread
        Dispatcher.Invoke(() => _viewModel.OnBrowserLoadingStateChanged(false));

        // Update screenshot if streaming mode is active
        if (_viewModel.IsStreamingMode) await CaptureStreamingScreenshotAsync();
    }

    /// <summary>
    /// Handles streaming mode toggle from ViewModel.
    /// </summary>
    /// <summary>
    /// Handles streaming mode toggle.
    /// </summary>
    /// <remarks>
    /// WebView2 is an HWND control with airspace issues - HWND controls always render
    /// on top of WPF content regardless of z-order. Must hide WebView2 for overlay to show,
    /// but capture screenshot first while browser is still visible.
    /// </remarks>
    private async void OnStreamingModeChanged(object? sender, EventArgs e)
    {
        if (_viewModel.IsStreamingMode)
        {
            // Capture screenshot BEFORE hiding browser (HWND must be visible to capture)
            await CaptureStreamingScreenshotAsync();
            // Now hide browser so WPF overlay becomes visible
            Browser.Visibility = System.Windows.Visibility.Collapsed;
        }
        else
        {
            // Show browser, clear screenshot
            Browser.Visibility = System.Windows.Visibility.Visible;
            _viewModel.StreamingScreenshot = null;
        }
    }

    /// <summary>
    /// Captures a screenshot of the WebView2 browser and applies Charcoal effect for streaming mode privacy.
    /// </summary>
    private async Task CaptureStreamingScreenshotAsync()
    {
        if (Browser.CoreWebView2 == null)
        {
            _logger.Warn("Cannot capture screenshot: CoreWebView2 is not initialized");
            return;
        }

        try
        {
            _logger.Debug("Capturing WebView2 screenshot for streaming mode");

            using var stream = new MemoryStream();
            await Browser.CoreWebView2.CapturePreviewAsync(
                CoreWebView2CapturePreviewImageFormat.Png, stream);

            stream.Position = 0;

            // Apply Charcoal effect using ImageMagick (runs on background thread)
            var processedBytes = await Task.Run(() => ApplyCharcoalEffect(stream));

            await Dispatcher.InvokeAsync(() =>
            {
                using var outputStream = new MemoryStream(processedBytes);
                var result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = outputStream;
                result.EndInit();
                result.Freeze();

                _viewModel.StreamingScreenshot = result;
                _logger.Debug("Screenshot captured and Charcoal effect applied successfully");
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to capture WebView2 screenshot");
        }
    }

    /// <summary>
    /// Captures screenshot with browser temporarily moved off-screen to avoid exposing content.
    /// </summary>
    /// <remarks>
    /// WebView2 is HWND control - must be visible to capture, but we move it off-screen
    /// so user doesn't see sensitive content during the brief capture window.
    /// </remarks>
    private async Task CaptureStreamingScreenshotOffScreenAsync()
    {
        if (Browser.CoreWebView2 == null)
            return;

        try
        {
            // Move browser off-screen (large negative offset)
            var originalTransform = Browser.RenderTransform;
            Browser.RenderTransform = new System.Windows.Media.TranslateTransform(-10000, 0);

            // Make visible (renders off-screen, not seen by user)
            Browser.Visibility = System.Windows.Visibility.Visible;
            await Task.Delay(50); // Allow render

            // Capture
            await CaptureStreamingScreenshotAsync();

            // Hide and restore transform
            Browser.Visibility = System.Windows.Visibility.Collapsed;
            Browser.RenderTransform = originalTransform;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to capture off-screen screenshot");
            // Ensure browser is hidden on error
            Browser.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Applies Charcoal sketch effect using ImageMagick for privacy distortion.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <returns>Byte array of the processed image.</returns>
    /// <remarks>
    /// <para>Creates a pencil/charcoal sketch appearance that emphasizes edges
    /// while removing color and detail - effective for privacy as text becomes unreadable.</para>
    /// <para><b>Radius</b> - Size of the Gaussian operator:</para>
    /// <list type="table">
    ///   <listheader><term>Value</term><description>Effect / Use Case</description></listheader>
    ///   <item><term>1-2</term><description>Fine lines, more detail preserved</description></item>
    ///   <item><term>3-5</term><description>Moderate sketch, good privacy balance</description></item>
    ///   <item><term>6+</term><description>Thick strokes, very abstract</description></item>
    /// </list>
    /// <para><b>Sigma</b> - Standard deviation of the Gaussian:</para>
    /// <list type="table">
    ///   <listheader><term>Value</term><description>Effect</description></listheader>
    ///   <item><term>0.5</term><description>Sharper edges</description></item>
    ///   <item><term>1.0</term><description>Balanced</description></item>
    ///   <item><term>2.0+</term><description>Softer, more blended edges</description></item>
    /// </list>
    /// </remarks>
    private static byte[] ApplyCharcoalEffect(Stream inputStream)
    {
        using var image = new MagickImage(inputStream);

        // Charcoal(radius, sigma): higher values = more abstract, text unreadable
        image.Charcoal(7, 4);

        using var outputStream = new MemoryStream();
        image.Write(outputStream, MagickFormat.Png);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Applies OilPaint effect using ImageMagick for privacy distortion.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <returns>Byte array of the processed image.</returns>
    /// <remarks>
    /// <para><b>Radius</b> - Brush/neighborhood size in pixels:</para>
    /// <list type="table">
    ///   <listheader><term>Value</term><description>Effect / Use Case</description></listheader>
    ///   <item><term>1-3</term><description>Subtle texture, details preserved - Light privacy, artistic touch</description></item>
    ///   <item><term>4-6</term><description>Moderate abstraction, text unreadable - Good balance</description></item>
    ///   <item><term>8-12</term><description>Heavy abstraction, shapes visible - Strong privacy</description></item>
    ///   <item><term>15+</term><description>Very abstract, only colors remain - Maximum privacy</description></item>
    /// </list>
    /// <para><b>Sigma</b> - Smoothness/blend factor:</para>
    /// <list type="table">
    ///   <listheader><term>Value</term><description>Effect</description></listheader>
    ///   <item><term>0.5</term><description>Sharper brush strokes, more texture</description></item>
    ///   <item><term>1.0</term><description>Balanced</description></item>
    ///   <item><term>2.0+</term><description>Smoother, more blended strokes</description></item>
    /// </list>
    /// </remarks>
    private static byte[] ApplyOilPaintEffect(Stream inputStream)
    {
        using var image = new MagickImage(inputStream);

        // OilPaint(radius, sigma): radius=3 for subtle effect, sigma=1 for balanced smoothness
        image.OilPaint(2, 2);

        using var outputStream = new MemoryStream();
        image.Write(outputStream, MagickFormat.Png);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Handles browser reload request from ViewModel.
    /// </summary>
    private void OnBrowserReloadRequested(object? sender, EventArgs e)
    {
        _logger.Debug("Browser reload requested from ViewModel");
        // Reload the browser using WebView2's native Reload method
        if (Browser.CoreWebView2 != null)
            Browser.CoreWebView2.Reload();
        else
            _logger.Warn("Cannot reload: CoreWebView2 is not initialized");
    }

    /// <summary>
    /// Handles intercepted fund data from network responses.
    /// </summary>
    private void OnFundDataIntercepted(object? sender, Models.FundDataInterceptedEventArgs e)
    {
        _logger.Info($"Fund data intercepted: {e.FundData?.Funds?.Count ?? 0} funds from {e.SourceUri}");

        // Forward to ViewModel on UI thread
        Dispatcher.Invoke(() => { _viewModel.OnFundDataReceived(e.FundData); });
    }

    /// <summary>
    /// Handles the request to load more funds by clicking the "Visa fler" button.
    /// </summary>
    private async void OnRequestLoadMoreFunds(object? sender, EventArgs e)
    {
        _logger.Info("Request to load more funds received - clicking 'Visa fler' button");

        if (Browser.CoreWebView2 == null)
        {
            _logger.Warn("Cannot load more funds: CoreWebView2 is not initialized");
            return;
        }

        try
        {
            // Note: IsPaginationInProgress flag is already set by the calling command
            // (LoadAllFundsCommand sets it to true, LoadNextBatchCommand leaves it false)

            // Wait a bit for the page to settle after the previous load
            await Task.Delay(500);

            // JavaScript to find and click the "Visa fler" button
            // The button typically has Swedish text "Visa fler"
            var clickButtonScript = @"
                (function() {
                    // Try multiple selectors to find the 'Visa fler' button

                    // Method 1: Find button by text content
                    const buttons = Array.from(document.querySelectorAll('button'));
                    const visaFlerButton = buttons.find(btn =>
                        btn.textContent && btn.textContent.toLowerCase().includes('visa fler')
                    );

                    if (visaFlerButton) {
                        visaFlerButton.click();
                        return 'Clicked Visa fler button (by text)';
                    }

                    // Method 2: Common class names for load more buttons
                    const loadMoreButton = document.querySelector('.load-more, .show-more, [data-testid*=""load""], [data-testid*=""more""]');
                    if (loadMoreButton) {
                        loadMoreButton.click();
                        return 'Clicked load more button (by class)';
                    }

                    // Method 3: Find by aria-label
                    const ariaButton = document.querySelector('[aria-label*=""visa""][aria-label*=""fler""], [aria-label*=""load""][aria-label*=""more""]');
                    if (ariaButton) {
                        ariaButton.click();
                        return 'Clicked button (by aria-label)';
                    }

                    return 'Button not found';
                })();
            ";

            var result = await Browser.CoreWebView2.ExecuteScriptAsync(clickButtonScript);
            _logger.Info($"Click button script result: {result}");

            // If button was not found, stop pagination
            if (result.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warn("'Visa fler' button not found on page. Stopping pagination.");
                // Only set to false if it was in progress (to stop "Load All" operation)
                if (_viewModel.IsPaginationInProgress) _viewModel.IsPaginationInProgress = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while trying to click 'Visa fler' button");
            _viewModel.IsPaginationInProgress = false;
        }
    }

    /// <summary>
    /// Handles the browser scroll to end request.
    /// Executes smooth scroll JavaScript in the WebView2 browser.
    /// Updates streaming screenshot after content has rendered.
    /// </summary>
    private async void OnBrowserScrollToEndRequested(object? sender, EventArgs e)
    {
        if (Browser?.CoreWebView2 == null)
        {
            _logger.Warn("Cannot scroll - WebView2 not initialized");
            return;
        }

        try
        {
            // Smooth scroll to bottom of page using modern JavaScript API
            // Alternative scroll approaches:
            //   - Scroll element into view: document.querySelector('selector').scrollIntoView({ behavior: 'smooth', block: 'end' });
            //   - Custom animation with requestAnimationFrame for fine-grained control over duration/easing
            //   - Instant scroll (no animation): window.scrollTo(0, document.body.scrollHeight);
            var scrollScript = @"
                (function() {
                    window.scrollTo({
                        top: document.body.scrollHeight,
                        behavior: 'smooth'
                    });
                    return 'scrolled';
                })();
            ";

            await Browser.CoreWebView2.ExecuteScriptAsync(scrollScript);
            _logger.Debug("Browser smooth scroll to bottom executed");

            // Update streaming screenshot after scroll and DOM render
            if (_viewModel.IsStreamingMode)
            {
                // Delay to allow scroll animation and DOM rendering to complete
                await Task.Delay(500);
                await CaptureStreamingScreenshotOffScreenAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to execute browser scroll");
        }
    }
}