using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using NLog;
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

            webView.CoreWebView2.NavigationCompleted += (s, e) => { viewModel.OnBrowserLoadingChanged(false); };

            Logger.Info("WebView2 initialized successfully for AboutFund");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize WebView2");
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