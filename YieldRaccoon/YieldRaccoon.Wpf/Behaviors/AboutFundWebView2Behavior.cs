using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NLog;
using YieldRaccoon.Wpf.Models;
using YieldRaccoon.Wpf.Services;
using YieldRaccoon.Wpf.ViewModels;

namespace YieldRaccoon.Wpf.Behaviors;

/// <summary>
/// Attached behavior for WebView2 in the AboutFund window.
/// Handles initialization, navigation events, and response interception.
/// </summary>
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

    public static AboutFundWindowViewModel? GetViewModel(DependencyObject obj) =>
        (AboutFundWindowViewModel?)obj.GetValue(ViewModelProperty);

    public static void SetViewModel(DependencyObject obj, AboutFundWindowViewModel? value) =>
        obj.SetValue(ViewModelProperty, value);

    #endregion

    #region Interceptor Storage Property

    private static readonly DependencyProperty InterceptorProperty =
        DependencyProperty.RegisterAttached(
            "Interceptor",
            typeof(IAboutFundResponseInterceptor),
            typeof(AboutFundWebView2Behavior),
            new PropertyMetadata(null));

    private static IAboutFundResponseInterceptor? GetInterceptor(DependencyObject obj) =>
        (IAboutFundResponseInterceptor?)obj.GetValue(InterceptorProperty);

    private static void SetInterceptor(DependencyObject obj, IAboutFundResponseInterceptor? value) =>
        obj.SetValue(InterceptorProperty, value);

    #endregion

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2 webView)
            return;

        // Cleanup old ViewModel subscriptions
        if (e.OldValue is AboutFundWindowViewModel oldViewModel)
        {
            DetachViewModel(webView, oldViewModel);
        }

        // Setup new ViewModel
        if (e.NewValue is AboutFundWindowViewModel newViewModel)
        {
            AttachViewModel(webView, newViewModel);
        }
    }

    private static void AttachViewModel(WebView2 webView, AboutFundWindowViewModel viewModel)
    {
        Logger.Debug("Attaching ViewModel to WebView2");

        // Subscribe to ViewModel events
        viewModel.BrowserReloadRequested += (s, e) => OnBrowserReloadRequested(webView);
        viewModel.NavigationRequested += (s, url) => OnNavigationRequested(webView, url);

        // Handle Loaded event for async initialization
        if (webView.IsLoaded)
        {
            _ = InitializeWebView2Async(webView, viewModel);
        }
        else
        {
            webView.Loaded += async (s, e) => await InitializeWebView2Async(webView, viewModel);
        }

        // Handle Unloaded for cleanup
        webView.Unloaded += (s, e) => CleanupWebView2(webView);
    }

    private static void DetachViewModel(WebView2 webView, AboutFundWindowViewModel viewModel)
    {
        Logger.Debug("Detaching ViewModel from WebView2");
        CleanupWebView2(webView);
    }

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

            // Wire up navigation events
            webView.CoreWebView2.NavigationStarting += (s, e) =>
            {
                viewModel.OnBrowserLoadingChanged(true);
            };

            webView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                viewModel.OnBrowserLoadingChanged(false);
            };

            // Create and initialize interceptor
            var interceptor = new AboutFundResponseInterceptor(Logger);
            interceptor.Initialize(webView);

            // Connect interceptor to ViewModel
            interceptor.RequestIntercepted += (s, request) =>
            {
                // Marshal to UI thread
                webView.Dispatcher.InvokeAsync(() => viewModel.OnRequestIntercepted(request));
            };

            // Store interceptor for cleanup
            SetInterceptor(webView, interceptor);

            Logger.Info("WebView2 initialized successfully for AboutFund");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize WebView2");
        }
    }

    private static void CleanupWebView2(WebView2 webView)
    {
        Logger.Debug("Cleaning up WebView2");

        var interceptor = GetInterceptor(webView);
        if (interceptor != null)
        {
            interceptor.Dispose();
            SetInterceptor(webView, null);
        }
    }

    private static void OnBrowserReloadRequested(WebView2 webView)
    {
        if (webView.CoreWebView2 != null)
        {
            Logger.Debug("Reloading browser");
            webView.CoreWebView2.Reload();
        }
    }

    private static void OnNavigationRequested(WebView2 webView, string url)
    {
        if (webView.CoreWebView2 != null && !string.IsNullOrWhiteSpace(url))
        {
            Logger.Debug("Navigating to: {0}", url);
            webView.CoreWebView2.Navigate(url);
        }
    }
}
