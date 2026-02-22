using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;
using NLog;
using YieldRaccoon.Wpf.Services;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf.Behaviors;

/// <summary>
/// Attached behavior for WebView2 in the AboutFund window.
/// Handles initialization, navigation events, and loading state notifications.
/// </summary>
/// <remarks>
/// This is a thin bridge that forwards navigation events between the WebView2 control
/// and <see cref="AboutFundWindowViewModel"/>. Page interaction logic (e.g., clicking elements,
/// executing scripts) belongs in dedicated services, not here.
/// </remarks>
public static class AboutFundWebView2Behavior
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Subject<(WebView2, AboutFundWindowViewModel)> PrivacyRefreshSubject = new();
    private static IDisposable? _privacyRefreshSubscription;
    private static IDisposable? _periodicRefreshSubscription;
    private static readonly SemaphoreSlim RefreshSemaphore = new(1, 1);

    #region ViewModel Attached Property

    /// <summary>
    /// Attached property to bind the ViewModel to the WebView2 control.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.RegisterAttached(
            "ViewModel",
            typeof(AboutFundWindowViewModel),
            typeof(AboutFundWebView2Behavior),
            new PropertyMetadata(null, OnViewModelChanged));

    /// <summary>
    /// Gets the <see cref="AboutFundWindowViewModel"/> attached to the specified <see cref="DependencyObject"/>.
    /// </summary>
    /// <param name="obj">The element to read the property from.</param>
    /// <returns>The attached ViewModel, or <see langword="null"/> if not set.</returns>
    public static AboutFundWindowViewModel? GetViewModel(DependencyObject obj)
    {
        return (AboutFundWindowViewModel?)obj.GetValue(ViewModelProperty);
    }

    /// <summary>
    /// Sets the <see cref="AboutFundWindowViewModel"/> on the specified <see cref="DependencyObject"/>.
    /// </summary>
    /// <param name="obj">The element to set the property on.</param>
    /// <param name="value">The ViewModel to attach.</param>
    public static void SetViewModel(DependencyObject obj, AboutFundWindowViewModel? value)
    {
        obj.SetValue(ViewModelProperty, value);
    }

    #endregion

    /// <summary>
    /// Handles <see cref="ViewModelProperty"/> changes — detaches from the old ViewModel and attaches to the new one.
    /// </summary>
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2 webView)
            return;

        // Cleanup old ViewModel subscriptions
        if (e.OldValue is AboutFundWindowViewModel oldViewModel) DetachViewModel(webView, oldViewModel);

        // Setup new ViewModel
        if (e.NewValue is AboutFundWindowViewModel newViewModel) AttachViewModel(webView, newViewModel);
    }

    /// <summary>
    /// Subscribes to ViewModel commands and kicks off WebView2 async initialization.
    /// </summary>
    private static void AttachViewModel(WebView2 webView, AboutFundWindowViewModel viewModel)
    {
        Logger.Debug("Attaching ViewModel to WebView2");

        // Subscribe to ViewModel events
        viewModel.BrowserReloadRequested += (s, e) => OnBrowserReloadRequested(webView);
        viewModel.NavigationRequested += (s, uri) => OnNavigationRequested(webView, uri);

        // Handle Loaded event for async initialization
        if (webView.IsLoaded)
            _ = InitializeWebView2Async(webView, viewModel);
        else
            webView.Loaded += async (s, e) => await InitializeWebView2Async(webView, viewModel);
    }

    /// <summary>
    /// Placeholder for unsubscribing from ViewModel events when the binding changes.
    /// </summary>
    private static void DetachViewModel(WebView2 webView, AboutFundWindowViewModel viewModel)
    {
        Logger.Debug("Detaching ViewModel from WebView2");
    }

    /// <summary>
    /// Ensures <c>CoreWebView2</c> is ready, then wires <c>NavigationStarting</c>/<c>NavigationCompleted</c>
    /// events to <see cref="AboutFundWindowViewModel.OnBrowserLoadingChanged"/>.
    /// </summary>
    private static async Task InitializeWebView2Async(WebView2 webView, AboutFundWindowViewModel viewModel)
    {
        try
        {
            Logger.Debug("Initializing WebView2");

            // Ensure CoreWebView2 is initialized
            await webView.EnsureCoreWebView2Async();

            if (webView.CoreWebView2 == null)
            {
                Logger.Error("CoreWebView2 is null after initialization");
                return;
            }

            // Wire up navigation events for loading state
            webView.CoreWebView2.NavigationStarting += (s, e) => { viewModel.OnBrowserLoadingChanged(true); };

            // Debounce privacy screenshot refresh — pages fire multiple NavigationCompleted
            // events (subframes, XHR redirects) and capturing too early yields a gray loading page.
            _privacyRefreshSubscription?.Dispose();
            _privacyRefreshSubscription = PrivacyRefreshSubject
                .Throttle(TimeSpan.FromMilliseconds(1500))
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(async args => await RefreshPrivacyScreenshotAsync(args.Item1, args.Item2));

            webView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                viewModel.OnBrowserLoadingChanged(false);

                if (viewModel.IsPrivacyMode && webView.CoreWebView2 != null)
                    PrivacyRefreshSubject.OnNext((webView, viewModel));
            };

            // Periodic refresh every 10 seconds — keeps the screenshot current when
            // page content updates dynamically (AJAX, charts) without triggering navigation.
            _periodicRefreshSubscription?.Dispose();
            _periodicRefreshSubscription = Observable.Interval(TimeSpan.FromSeconds(10))
                .ObserveOn(SynchronizationContext.Current!)
                .Where(_ => viewModel.IsPrivacyMode && webView.CoreWebView2 != null)
                .Subscribe(async _ => await RefreshPrivacyScreenshotAsync(webView, viewModel));

            Logger.Info("WebView2 initialized successfully for AboutFund");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize WebView2");
        }
    }

    /// <summary>
    /// Refreshes the privacy screenshot by temporarily showing the browser off-screen.
    /// </summary>
    /// <remarks>
    /// WebView2 is an HWND control — <c>CapturePreviewAsync</c> requires the control to be visible.
    /// We move it off-screen via <see cref="TranslateTransform"/> so the user never sees live content.
    /// </remarks>
    private static async Task RefreshPrivacyScreenshotAsync(WebView2 webView, AboutFundWindowViewModel viewModel)
    {
        if (!await RefreshSemaphore.WaitAsync(0))
            return;

        try
        {
            // Move browser off-screen, make visible for capture
            var originalTransform = webView.RenderTransform;
            webView.RenderTransform = new TranslateTransform(-10000, 0);
            webView.Visibility = Visibility.Visible;
            await Task.Delay(50); // Allow render

            viewModel.PrivacyScreenshot = await PrivacyFilterService.CaptureAndFilterAsync(
                webView.CoreWebView2!, webView.Dispatcher);

            // Restore hidden state
            webView.Visibility = Visibility.Collapsed;
            webView.RenderTransform = originalTransform;

            Logger.Debug("Privacy screenshot refreshed after navigation");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to refresh privacy screenshot");
            webView.Visibility = Visibility.Collapsed;
        }
        finally
        {
            RefreshSemaphore.Release();
        }
    }

    /// <summary>
    /// Handles a reload request from the ViewModel by calling <see cref="CoreWebView2.Reload"/>.
    /// </summary>
    private static void OnBrowserReloadRequested(WebView2 webView)
    {
        if (webView.CoreWebView2 != null)
        {
            Logger.Debug("Reloading browser");
            webView.CoreWebView2.Reload();
        }
    }

    /// <summary>
    /// Handles a navigation request from the ViewModel by calling <see cref="CoreWebView2.Navigate"/>.
    /// Converts the <see cref="Uri"/> to string at the WebView2 boundary.
    /// </summary>
    private static void OnNavigationRequested(WebView2 webView, Uri uri)
    {
        if (webView.CoreWebView2 != null)
        {
            Logger.Debug("Navigating to: {0}", uri);
            webView.CoreWebView2.Navigate(uri.ToString());
        }
    }
}