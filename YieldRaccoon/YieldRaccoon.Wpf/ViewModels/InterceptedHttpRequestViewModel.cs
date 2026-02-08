using DevExpress.Mvvm;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel wrapper for <see cref="InterceptedHttpRequest"/> that implements INotifyPropertyChanged
/// for proper WPF data binding and change notification.
/// </summary>
public class InterceptedHttpRequestViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the unique identifier for this request.
    /// </summary>
    public string Id
    {
        get => GetValue<string>() ?? string.Empty;
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the timestamp when the response was received.
    /// </summary>
    public DateTime Timestamp
    {
        get => GetValue<DateTime>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the HTTP method (GET, POST, etc.).
    /// </summary>
    public string Method
    {
        get => GetValue<string>() ?? string.Empty;
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the request URL.
    /// </summary>
    public string Url
    {
        get => GetValue<string>() ?? string.Empty;
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the reason phrase of the response.
    /// </summary>
    public string StatusText
    {
        get => GetValue<string>() ?? string.Empty;
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the Content-Type header value.
    /// </summary>
    public string ContentType
    {
        get => GetValue<string>() ?? string.Empty;
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the Content-Length in bytes.
    /// </summary>
    public long ContentLength
    {
        get => GetValue<long>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets a preview of the response content.
    /// </summary>
    public string? ResponsePreview
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets whether the response status indicates success (2xx).
    /// </summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

    /// <summary>
    /// Gets whether the response status indicates a client error (4xx).
    /// </summary>
    public bool IsClientError => StatusCode >= 400 && StatusCode < 500;

    /// <summary>
    /// Gets whether the response status indicates a server error (5xx).
    /// </summary>
    public bool IsServerError => StatusCode >= 500;

    /// <summary>
    /// Gets a shortened URL for display (truncated to 80 characters).
    /// </summary>
    public string ShortUrl => Url.Length > 80 ? Url[..77] + "..." : Url;

    /// <summary>
    /// Updates this ViewModel from an <see cref="InterceptedHttpRequest"/> model.
    /// </summary>
    /// <param name="request">The source request to update from.</param>
    public void UpdateFrom(InterceptedHttpRequest request)
    {
        Id = request.Id;
        Timestamp = request.Timestamp;
        Method = request.Method;
        Url = request.Url;
        StatusCode = request.StatusCode;
        StatusText = request.StatusText;
        ContentType = request.ContentType;
        ContentLength = request.ContentLength;
        ResponsePreview = request.ResponsePreview;

        // Raise property changed for computed properties
        RaisePropertyChanged(nameof(IsSuccess));
        RaisePropertyChanged(nameof(IsClientError));
        RaisePropertyChanged(nameof(IsServerError));
        RaisePropertyChanged(nameof(ShortUrl));
    }

    /// <summary>
    /// Creates a new <see cref="InterceptedHttpRequestViewModel"/> from an <see cref="InterceptedHttpRequest"/>.
    /// </summary>
    /// <param name="request">The source request.</param>
    /// <returns>A new ViewModel instance.</returns>
    public static InterceptedHttpRequestViewModel FromModel(InterceptedHttpRequest request)
    {
        var vm = new InterceptedHttpRequestViewModel();
        vm.UpdateFrom(request);
        return vm;
    }
}
