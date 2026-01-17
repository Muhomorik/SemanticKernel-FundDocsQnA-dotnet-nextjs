using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;

namespace PdfTextExtractor.Wpf.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger _logger;
    private readonly IScheduler _uiScheduler;
    private readonly CompositeDisposable _disposables = new();

    private string _title = "PDF Text Extractor";
    private string _status = "Ready";

    /// <summary>
    /// Runtime constructor with dependency injection.
    /// </summary>
    /// <param name="logger">Logger instance for this ViewModel.</param>
    /// <param name="uiScheduler">Scheduler for UI thread marshalling.</param>
    public MainViewModel(ILogger logger, IScheduler uiScheduler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uiScheduler = uiScheduler ?? throw new ArgumentNullException(nameof(uiScheduler));

        // Initialize commands
        LoadedCommand = new DelegateCommand(OnLoaded);
    }

    /// <summary>
    /// Design-time constructor for XAML designer support.
    /// </summary>
    public MainViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _uiScheduler = DispatcherScheduler.Current;

        // Initialize commands with no-op implementations for designer
        LoadedCommand = new DelegateCommand(() => { });
    }

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value, nameof(Title));
    }

    /// <summary>
    /// Gets or sets the current status message.
    /// </summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value, nameof(Status));
    }

    /// <summary>
    /// Command executed when the window is loaded.
    /// </summary>
    public ICommand LoadedCommand { get; }

    /// <summary>
    /// Handles the window loaded event.
    /// </summary>
    private void OnLoaded()
    {
        _logger.Info("MainViewModel loaded");
        Status = "Application ready";

        // TODO: Initialize subscriptions and load initial data here
        // Example:
        // Observable
        //     .Interval(TimeSpan.FromSeconds(1))
        //     .ObserveOn(_uiScheduler)
        //     .Subscribe(_ => UpdateStatus())
        //     .DisposeWith(_disposables);
    }

    /// <summary>
    /// Disposes resources used by this ViewModel.
    /// </summary>
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
