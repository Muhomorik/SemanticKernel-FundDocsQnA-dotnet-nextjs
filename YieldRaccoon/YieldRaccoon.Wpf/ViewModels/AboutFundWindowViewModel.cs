using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the AboutFund browser window with network request interception.
/// </summary>
public class AboutFundWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger _logger;
    private readonly YieldRaccoonOptions _options;
    private bool _disposed;

    /// <summary>
    /// Event raised when the window should close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Event raised when browser reload is requested.
    /// </summary>
    public event EventHandler? BrowserReloadRequested;

    /// <summary>
    /// Event raised when navigation is requested.
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    #region Properties

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title
    {
        get => GetProperty(() => Title);
        set => SetProperty(() => Title, value);
    }

    /// <summary>
    /// Gets or sets the browser URL.
    /// </summary>
    public string BrowserUrl
    {
        get => GetProperty(() => BrowserUrl);
        set => SetProperty(() => BrowserUrl, value);
    }

    /// <summary>
    /// Gets or sets the URL filter for the interceptor panel.
    /// </summary>
    public string UrlFilter
    {
        get => GetProperty(() => UrlFilter);
        set
        {
            if (SetProperty(() => UrlFilter, value)) ApplyFilter();
        }
    }

    /// <summary>
    /// Gets or sets whether request interception is enabled.
    /// </summary>
    public bool IsInterceptorEnabled
    {
        get => GetProperty(() => IsInterceptorEnabled);
        set => SetProperty(() => IsInterceptorEnabled, value);
    }

    /// <summary>
    /// Gets or sets whether the browser is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get => GetProperty(() => IsLoading);
        set => SetProperty(() => IsLoading, value);
    }

    /// <summary>
    /// Gets or sets the currently selected request.
    /// </summary>
    public AboutFundInterceptedRequestViewModel? SelectedRequest
    {
        get => GetProperty(() => SelectedRequest);
        set
        {
            if (SetProperty(() => SelectedRequest, value)) CommandManager.InvalidateRequerySuggested();
        }
    }

    #endregion

    #region Collections

    /// <summary>
    /// Gets all intercepted requests (unfiltered).
    /// </summary>
    public ObservableCollection<AboutFundInterceptedRequestViewModel> InterceptedRequests { get; } = new();

    /// <summary>
    /// Gets the filtered requests based on UrlFilter.
    /// </summary>
    public ObservableCollection<AboutFundInterceptedRequestViewModel> FilteredRequests { get; } = new();

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to navigate to a URL.
    /// </summary>
    public ICommand NavigateCommand { get; }

    /// <summary>
    /// Gets the command to reload the browser.
    /// </summary>
    public ICommand ReloadCommand { get; }

    /// <summary>
    /// Gets the command to clear all intercepted requests.
    /// </summary>
    public ICommand ClearRequestsCommand { get; }

    /// <summary>
    /// Gets the command to close the window.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Gets the command to copy the selected request URL.
    /// </summary>
    public ICommand CopyUrlCommand { get; }

    /// <summary>
    /// Gets the command to copy the selected request response.
    /// </summary>
    public ICommand CopyResponseCommand { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundWindowViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="options">Configuration options containing URL templates.</param>
    public AboutFundWindowViewModel(ILogger logger, YieldRaccoonOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        Title = "AboutFund - Network Inspector";
        BrowserUrl = _options.FundDetailsPageUrlTemplate;
        UrlFilter = string.Empty;
        IsInterceptorEnabled = true;
        IsLoading = false;

        // Initialize commands
        NavigateCommand = new DelegateCommand(ExecuteNavigate);
        ReloadCommand = new DelegateCommand(ExecuteReload);
        ClearRequestsCommand = new DelegateCommand(ExecuteClearRequests);
        CloseCommand = new DelegateCommand(ExecuteClose);
        CopyUrlCommand = new DelegateCommand(ExecuteCopyUrl, CanExecuteCopyUrl, true);
        CopyResponseCommand = new DelegateCommand(ExecuteCopyResponse, CanExecuteCopyResponse, true);

        _logger.Debug("AboutFundWindowViewModel initialized");
    }

    /// <summary>
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public AboutFundWindowViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _options = new YieldRaccoonOptions { FundDetailsPageUrlTemplate = "https://example.com/" };

        Title = "AboutFund - Network Inspector (Design)";
        BrowserUrl = _options.FundDetailsPageUrlTemplate;
        UrlFilter = string.Empty;
        IsInterceptorEnabled = true;
        IsLoading = false;

        NavigateCommand = new DelegateCommand(() => { });
        ReloadCommand = new DelegateCommand(() => { });
        ClearRequestsCommand = new DelegateCommand(() => { });
        CloseCommand = new DelegateCommand(() => { });
        CopyUrlCommand = new DelegateCommand(() => { });
        CopyResponseCommand = new DelegateCommand(() => { });

        // Add sample data for design time
        var sampleRequest = new AboutFundInterceptedRequestViewModel
        {
            Timestamp = DateTime.Now,
            Method = "GET",
            Url = "https://example.com/",
            StatusCode = 200,
            ContentType = "application/json"
        };
        InterceptedRequests.Add(sampleRequest);
        FilteredRequests.Add(sampleRequest);
    }

    #region Public Methods

    /// <summary>
    /// Called when a request is intercepted by the interceptor service.
    /// </summary>
    /// <param name="request">The intercepted request.</param>
    public void OnRequestIntercepted(AboutFundInterceptedRequest request)
    {
        if (!IsInterceptorEnabled) return;

        var viewModel = AboutFundInterceptedRequestViewModel.FromModel(request);

        // Insert at the beginning (most recent first)
        InterceptedRequests.Insert(0, viewModel);

        // Apply filter
        if (MatchesFilter(viewModel)) FilteredRequests.Insert(0, viewModel);

        _logger.Trace("Intercepted: {0} {1} -> {2}", request.Method, request.Url, request.StatusCode);
    }

    /// <summary>
    /// Called when browser loading state changes.
    /// </summary>
    /// <param name="isLoading">Whether the browser is loading.</param>
    public void OnBrowserLoadingChanged(bool isLoading)
    {
        IsLoading = isLoading;
    }

    #endregion

    #region Command Implementations

    private void ExecuteNavigate()
    {
        if (!string.IsNullOrWhiteSpace(BrowserUrl))
        {
            _logger.Info("Navigating to: {0}", BrowserUrl);
            NavigationRequested?.Invoke(this, BrowserUrl);
        }
    }

    private void ExecuteReload()
    {
        _logger.Debug("Reload requested");
        BrowserReloadRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteClearRequests()
    {
        _logger.Debug("Clearing requests");
        InterceptedRequests.Clear();
        FilteredRequests.Clear();
        SelectedRequest = null;
    }

    private void ExecuteClose()
    {
        _logger.Debug("Close requested");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanExecuteCopyUrl()
    {
        return SelectedRequest != null;
    }

    private void ExecuteCopyUrl()
    {
        if (SelectedRequest != null)
        {
            Clipboard.SetText(SelectedRequest.Url);
            _logger.Debug("Copied URL to clipboard: {0}", SelectedRequest.Url);
        }
    }

    private bool CanExecuteCopyResponse()
    {
        return SelectedRequest?.ResponsePreview != null;
    }

    private void ExecuteCopyResponse()
    {
        if (SelectedRequest?.ResponsePreview != null)
        {
            Clipboard.SetText(SelectedRequest.ResponsePreview);
            _logger.Debug("Copied response to clipboard");
        }
    }

    #endregion

    #region Filter Logic

    private void ApplyFilter()
    {
        FilteredRequests.Clear();

        foreach (var request in InterceptedRequests)
            if (MatchesFilter(request))
                FilteredRequests.Add(request);
    }

    private bool MatchesFilter(AboutFundInterceptedRequestViewModel request)
    {
        if (string.IsNullOrWhiteSpace(UrlFilter))
            return true;

        return request.Url.Contains(UrlFilter, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing) _logger.Debug("AboutFundWindowViewModel disposing");

        _disposed = true;
    }

    #endregion
}