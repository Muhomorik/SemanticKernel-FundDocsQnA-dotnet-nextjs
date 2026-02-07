using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// Reusable ViewModel for the network request interceptor panel.
/// </summary>
/// <remarks>
/// Extracted from <c>AboutFundWindowViewModel</c> for reuse across multiple windows.
/// </remarks>
public class InterceptorViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger _logger;
    private bool _disposed;

    #region Properties

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
    /// Gets or sets the currently selected request.
    /// </summary>
    public InterceptedHttpRequestViewModel? SelectedRequest
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
    public ObservableCollection<InterceptedHttpRequestViewModel> InterceptedRequests { get; } = new();

    /// <summary>
    /// Gets the filtered requests based on UrlFilter.
    /// </summary>
    public ObservableCollection<InterceptedHttpRequestViewModel> FilteredRequests { get; } = new();

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to clear all intercepted requests.
    /// </summary>
    public ICommand ClearRequestsCommand { get; }

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
    /// Initializes a new instance of the <see cref="InterceptorViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InterceptorViewModel(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        UrlFilter = string.Empty;
        IsInterceptorEnabled = true;

        ClearRequestsCommand = new DelegateCommand(ExecuteClearRequests);
        CopyUrlCommand = new DelegateCommand(ExecuteCopyUrl, CanExecuteCopyUrl, true);
        CopyResponseCommand = new DelegateCommand(ExecuteCopyResponse, CanExecuteCopyResponse, true);

        _logger.Debug("InterceptorViewModel initialized");
    }

    #region Public Methods

    /// <summary>
    /// Called when a request is intercepted by the interceptor service.
    /// </summary>
    /// <param name="request">The intercepted request.</param>
    public void OnRequestIntercepted(InterceptedHttpRequest request)
    {
        if (!IsInterceptorEnabled) return;

        var viewModel = InterceptedHttpRequestViewModel.FromModel(request);

        InterceptedRequests.Insert(0, viewModel);

        if (MatchesFilter(viewModel)) FilteredRequests.Insert(0, viewModel);

        _logger.Trace("Intercepted: {0} {1} -> {2}", request.Method, request.Url, request.StatusCode);
    }

    #endregion

    #region Command Implementations

    private void ExecuteClearRequests()
    {
        _logger.Debug("Clearing requests");
        InterceptedRequests.Clear();
        FilteredRequests.Clear();
        SelectedRequest = null;
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

    private bool MatchesFilter(InterceptedHttpRequestViewModel request)
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
        if (_disposed) return;

        if (disposing) _logger.Debug("InterceptorViewModel disposing");

        _disposed = true;
    }

    #endregion
}
