using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;
using PdfTextExtractor.Core;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Batch;
using PdfTextExtractor.Core.Domain.Events.Document;
using PdfTextExtractor.Core.Domain.Events.Infrastructure;
using PdfTextExtractor.Core.Domain.Events.Ocr;
using PdfTextExtractor.Core.Domain.Events.Page;

namespace PdfTextExtractor.Wpf.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger _logger;
    private readonly IScheduler _uiScheduler;
    private readonly IPdfTextExtractorLib _extractorLib;
    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource? _cancellationTokenSource;

    // Window properties
    private string _title = "PDF Text Extractor";
    private string _status = "Ready";

    // Collections
    private ObservableCollection<PdfDocumentGroupViewModel> _pdfDocuments = new();

    // Settings
    private string _inputFolderPath = "";
    private string _outputFolderPath = "";
    private string _lmStudioUrl = "http://localhost:1234";
    private string _visionModelName = "qwen/qwen2.5-vl-7b";
    private int _dpi = 300;
    private int _chunkSize = 1000;

    // State
    private bool _isExtracting;
    private double _overallProgress;
    private string _extractedText = "";

    /// <summary>
    /// Runtime constructor with dependency injection.
    /// </summary>
    /// <param name="logger">Logger instance for this ViewModel.</param>
    /// <param name="uiScheduler">Scheduler for UI thread marshalling.</param>
    /// <param name="extractorLib">PDF text extractor library instance.</param>
    public MainViewModel(ILogger logger, IScheduler uiScheduler, IPdfTextExtractorLib extractorLib)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uiScheduler = uiScheduler ?? throw new ArgumentNullException(nameof(uiScheduler));
        _extractorLib = extractorLib ?? throw new ArgumentNullException(nameof(extractorLib));

        // Initialize commands
        LoadedCommand = new DelegateCommand(OnLoaded);
        BrowseInputFolderCommand = new DelegateCommand(OnBrowseInputFolder);
        BrowseOutputFolderCommand = new DelegateCommand(OnBrowseOutputFolder);
        StartExtractionCommand = new AsyncCommand(StartExtractionAsync, CanStartExtraction);
        CancelExtractionCommand = new DelegateCommand(OnCancelExtraction, () => IsExtracting);
        SetDpiCommand = new DelegateCommand<string>(OnSetDpi);
    }

    /// <summary>
    /// Design-time constructor for XAML designer support.
    /// </summary>
    public MainViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _uiScheduler = DispatcherScheduler.Current;
        _extractorLib = new PdfTextExtractorLib();

        // Initialize commands with no-op implementations for designer
        LoadedCommand = new DelegateCommand(() => { });
        BrowseInputFolderCommand = new DelegateCommand(() => { });
        BrowseOutputFolderCommand = new DelegateCommand(() => { });
        StartExtractionCommand = new AsyncCommand(async () => { });
        CancelExtractionCommand = new DelegateCommand(() => { });
        SetDpiCommand = new DelegateCommand<string>(_ => { });
    }

    #region Properties

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value, nameof(Title));
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value, nameof(Status));
    }

    public ObservableCollection<PdfDocumentGroupViewModel> PdfDocuments
    {
        get => _pdfDocuments;
        set => SetProperty(ref _pdfDocuments, value, nameof(PdfDocuments));
    }

    public string InputFolderPath
    {
        get => _inputFolderPath;
        set => SetProperty(ref _inputFolderPath, value, nameof(InputFolderPath));
    }

    public string OutputFolderPath
    {
        get => _outputFolderPath;
        set => SetProperty(ref _outputFolderPath, value, nameof(OutputFolderPath));
    }

    public string LMStudioUrl
    {
        get => _lmStudioUrl;
        set => SetProperty(ref _lmStudioUrl, value, nameof(LMStudioUrl));
    }

    public string VisionModelName
    {
        get => _visionModelName;
        set => SetProperty(ref _visionModelName, value, nameof(VisionModelName));
    }

    public int Dpi
    {
        get => _dpi;
        set => SetProperty(ref _dpi, value, nameof(Dpi));
    }

    public int ChunkSize
    {
        get => _chunkSize;
        set => SetProperty(ref _chunkSize, value, nameof(ChunkSize));
    }

    public bool IsExtracting
    {
        get => _isExtracting;
        set => SetProperty(ref _isExtracting, value, nameof(IsExtracting));
    }

    public double OverallProgress
    {
        get => _overallProgress;
        set => SetProperty(ref _overallProgress, value, nameof(OverallProgress));
    }

    public string ExtractedText
    {
        get => _extractedText;
        set => SetProperty(ref _extractedText, value, nameof(ExtractedText));
    }

    #endregion

    #region Commands

    public ICommand LoadedCommand { get; }
    public ICommand BrowseInputFolderCommand { get; }
    public ICommand BrowseOutputFolderCommand { get; }
    public ICommand StartExtractionCommand { get; }
    public ICommand CancelExtractionCommand { get; }
    public ICommand SetDpiCommand { get; }

    #endregion

    #region Event Handling

    private void OnLoaded()
    {
        _logger.Info("MainViewModel loaded");
        Status = "Application ready";

        // Subscribe to extraction events
        var subscription = _extractorLib.Events
            .ObserveOn(_uiScheduler)  // CRITICAL: Marshal to UI thread
            .Subscribe(OnExtractionEvent, OnEventError);

        _disposables.Add(subscription);
    }

    private void OnExtractionEvent(PdfExtractionEventBase evt)
    {
        switch (evt)
        {
            case BatchExtractionStarted e:
                HandleBatchStarted(e);
                break;
            case DocumentExtractionStarted e:
                HandleDocumentStarted(e);
                break;
            case PageRasterizationStarted e:
                HandlePageRasterizationStarted(e);
                break;
            case PageRasterizationCompleted e:
                HandlePageRasterizationCompleted(e);
                break;
            case OcrProcessingCompleted e:
                HandleOcrProcessingCompleted(e);
                break;
            case PageExtractionStarted e:
                HandlePageExtractionStarted(e);
                break;
            case PageExtractionCompleted e:
                HandlePageExtractionCompleted(e);
                break;
            case DocumentExtractionCompleted e:
                HandleDocumentCompleted(e);
                break;
            case BatchExtractionCompleted e:
                HandleBatchCompleted(e);
                break;
            case PageExtractionFailed e:
                HandlePageFailed(e);
                break;
            case ExtractionProgressUpdated e:
                HandleProgressUpdated(e);
                break;
        }
    }

    private void OnEventError(Exception ex)
    {
        _logger.Error(ex, "Error in event stream");
        Status = $"Error: {ex.Message}";
    }

    private void HandleBatchStarted(BatchExtractionStarted e)
    {
        IsExtracting = true;
        PdfDocuments.Clear();

        // Pre-create document groups
        foreach (var filePath in e.FilePaths)
        {
            PdfDocuments.Add(new PdfDocumentGroupViewModel
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            });
        }

        Status = $"Batch extraction started: {e.TotalFiles} file(s)";
        _logger.Info($"Batch extraction started: {e.TotalFiles} files");
    }

    private void HandleDocumentStarted(DocumentExtractionStarted e)
    {
        ExtractedText = "";  // Clear for new document
        Status = $"Processing: {e.FileName}";
        _logger.Info($"Document extraction started: {e.FileName}");
    }

    private void HandlePageRasterizationStarted(PageRasterizationStarted e)
    {
        var docGroup = PdfDocuments.FirstOrDefault(d => d.FilePath == e.FilePath);
        if (docGroup == null) return;

        var existingPage = docGroup.Pages.FirstOrDefault(p => p.PageNumber == e.PageNumber);
        if (existingPage == null)
        {
            docGroup.Pages.Add(new PagePreviewViewModel
            {
                FilePath = e.FilePath,
                PdfFileName = Path.GetFileName(e.FilePath),
                PageNumber = e.PageNumber,
                CorrelationId = e.CorrelationId,
                Status = PageStatus.Rasterizing
            });
        }
        else
        {
            existingPage.Status = PageStatus.Rasterizing;
        }
    }

    private void HandlePageRasterizationCompleted(PageRasterizationCompleted e)
    {
        var page = FindPage(e.FilePath, e.PageNumber);
        if (page != null)
        {
            page.ImagePath = e.TempImagePath;
            page.Status = PageStatus.Rasterizing;
        }
        else
        {
            // Page not yet created, add it now
            var docGroup = PdfDocuments.FirstOrDefault(d => d.FilePath == e.FilePath);
            if (docGroup != null)
            {
                docGroup.Pages.Add(new PagePreviewViewModel
                {
                    FilePath = e.FilePath,
                    PdfFileName = Path.GetFileName(e.FilePath),
                    PageNumber = e.PageNumber,
                    CorrelationId = e.CorrelationId,
                    ImagePath = e.TempImagePath,
                    Status = PageStatus.Rasterizing
                });
            }
        }
    }

    private void HandleOcrProcessingCompleted(OcrProcessingCompleted e)
    {
        // Append extracted text to the text viewer
        if (!string.IsNullOrWhiteSpace(e.ExtractedText))
        {
            ExtractedText += $"--- Page {e.PageNumber} ---{Environment.NewLine}{e.ExtractedText}{Environment.NewLine}{Environment.NewLine}";
        }
    }

    private void HandlePageExtractionStarted(PageExtractionStarted e)
    {
        var page = FindPage(e.FilePath, e.PageNumber);
        if (page != null)
        {
            page.Status = PageStatus.Extracting;
        }
    }

    private void HandlePageExtractionCompleted(PageExtractionCompleted e)
    {
        var page = FindPage(e.FilePath, e.PageNumber);
        if (page != null)
        {
            page.Status = PageStatus.Completed;
        }
    }

    private void HandleDocumentCompleted(DocumentExtractionCompleted e)
    {
        Status = $"Completed: {Path.GetFileName(e.FilePath)} ({e.TotalPages} pages, {e.Duration.TotalSeconds:F1}s)";
        _logger.Info($"Document extraction completed: {e.FilePath}");
    }

    private void HandleBatchCompleted(BatchExtractionCompleted e)
    {
        IsExtracting = false;
        Status = $"Batch extraction completed: {e.TotalFilesProcessed} file(s) in {e.TotalDuration.TotalSeconds:F1}s";
        _logger.Info($"Batch extraction completed: {e.TotalFilesProcessed} files");
    }

    private void HandlePageFailed(PageExtractionFailed e)
    {
        var page = FindPage(e.FilePath, e.PageNumber);
        if (page != null)
        {
            page.Status = PageStatus.Failed;
        }

        _logger.Error($"Page extraction failed: {e.FilePath}, Page {e.PageNumber}, Error: {e.ErrorMessage}");
    }

    private void HandleProgressUpdated(ExtractionProgressUpdated e)
    {
        OverallProgress = e.OverallPercentage;
        Status = $"{e.CurrentOperation} ({e.PagesProcessed}/{e.TotalPages})";
    }

    private PagePreviewViewModel? FindPage(string filePath, int pageNumber)
    {
        return PdfDocuments
            .FirstOrDefault(d => d.FilePath == filePath)
            ?.Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
    }

    #endregion

    #region Command Implementations

    private void OnBrowseInputFolder()
    {
        // Use Windows Forms FolderBrowserDialog (WPF doesn't have one)
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select PDF input folder"
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InputFolderPath = dialog.SelectedPath;
            _logger.Info($"Input folder selected: {dialog.SelectedPath}");
        }
    }

    private void OnBrowseOutputFolder()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select output folder"
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputFolderPath = dialog.SelectedPath;
            _logger.Info($"Output folder selected: {dialog.SelectedPath}");
        }
    }

    private bool CanStartExtraction()
    {
        return !IsExtracting
            && !string.IsNullOrWhiteSpace(InputFolderPath)
            && !string.IsNullOrWhiteSpace(OutputFolderPath)
            && !string.IsNullOrWhiteSpace(VisionModelName);
    }

    private async Task StartExtractionAsync()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Status = "Starting extraction...";
            _logger.Info("Starting LM Studio extraction");

            var parameters = new LMStudioParameters
            {
                PdfFolderPath = InputFolderPath,
                OutputFolderPath = OutputFolderPath,
                LMStudioUrl = LMStudioUrl,
                VisionModelName = VisionModelName,
                RasterizationDpi = Dpi,
                ChunkSize = ChunkSize
            };

            await _extractorLib.ExtractWithLMStudioAsync(parameters, _cancellationTokenSource.Token);

            Status = "Extraction completed successfully";
            _logger.Info("Extraction completed successfully");
        }
        catch (OperationCanceledException)
        {
            Status = "Extraction cancelled";
            _logger.Info("Extraction cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Extraction failed");
            Status = $"Extraction failed: {ex.Message}";
        }
        finally
        {
            IsExtracting = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void OnCancelExtraction()
    {
        _cancellationTokenSource?.Cancel();
        _logger.Info("Cancellation requested");
    }

    private void OnSetDpi(string? dpiValue)
    {
        if (!string.IsNullOrWhiteSpace(dpiValue) && int.TryParse(dpiValue, out int dpi))
        {
            Dpi = dpi;
            _logger.Info($"DPI preset selected: {dpi}");
            Status = $"DPI set to {dpi}";
        }
    }

    #endregion

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _disposables.Dispose();
        (_extractorLib as IDisposable)?.Dispose();
    }
}
