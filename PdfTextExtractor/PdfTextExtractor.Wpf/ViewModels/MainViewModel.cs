using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    private TextExtractionMethod _selectedMethod = TextExtractionMethod.PdfPig;

    // Configuration ViewModels
    public PdfPigConfigViewModel PdfPigConfig { get; }
    public LMStudioConfigViewModel LMStudioConfig { get; }
    public OpenAIConfigViewModel OpenAIConfig { get; }

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

        // Initialize configuration ViewModels
        PdfPigConfig = new PdfPigConfigViewModel();
        LMStudioConfig = new LMStudioConfigViewModel();
        OpenAIConfig = new OpenAIConfigViewModel();

        // Subscribe to child ViewModel property changes
        PdfPigConfig.PropertyChanged += OnChildConfigChanged;
        LMStudioConfig.PropertyChanged += OnChildConfigChanged;
        OpenAIConfig.PropertyChanged += OnChildConfigChanged;

        // Initialize commands
        LoadedCommand = new DelegateCommand(OnLoaded);
        BrowseInputFolderCommand = new DelegateCommand(OnBrowseInputFolder);
        BrowseOutputFolderCommand = new DelegateCommand(OnBrowseOutputFolder);
        StartExtractionCommand = new AsyncCommand(StartExtractionAsync, CanStartExtraction);
        CancelExtractionCommand = new DelegateCommand(OnCancelExtraction, () => IsExtracting);
    }

    /// <summary>
    /// Design-time constructor for XAML designer support.
    /// </summary>
    public MainViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _uiScheduler = DispatcherScheduler.Current;
        _extractorLib = new PdfTextExtractorLib();

        // Initialize configuration ViewModels
        PdfPigConfig = new PdfPigConfigViewModel();
        LMStudioConfig = new LMStudioConfigViewModel();
        OpenAIConfig = new OpenAIConfigViewModel();

        // Initialize commands with no-op implementations for designer
        LoadedCommand = new DelegateCommand(() => { });
        BrowseInputFolderCommand = new DelegateCommand(() => { });
        BrowseOutputFolderCommand = new DelegateCommand(() => { });
        StartExtractionCommand = new AsyncCommand(async () => { });
        CancelExtractionCommand = new DelegateCommand(() => { });
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

    public TextExtractionMethod SelectedMethod
    {
        get => _selectedMethod;
        set
        {
            if (SetProperty(ref _selectedMethod, value, nameof(SelectedMethod)))
            {
                // Notify visibility properties when method changes
                RaisePropertyChanged(nameof(IsPdfPigSelected));
                RaisePropertyChanged(nameof(IsLMStudioSelected));
                RaisePropertyChanged(nameof(IsOpenAISelected));
            }
        }
    }

    // Helper properties for XAML binding
    public bool IsPdfPigSelected => SelectedMethod == TextExtractionMethod.PdfPig;
    public bool IsLMStudioSelected => SelectedMethod == TextExtractionMethod.LMStudio;
    public bool IsOpenAISelected => SelectedMethod == TextExtractionMethod.OpenAI;

    // ComboBox items source
    public IEnumerable<TextExtractionMethod> AvailableMethods => new[]
    {
        TextExtractionMethod.PdfPig,
        TextExtractionMethod.LMStudio,
        TextExtractionMethod.OpenAI
    };

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
            case BatchExtractionFailed e:
                HandleBatchFailed(e);
                break;
            case BatchExtractionCancelled e:
                HandleBatchCancelled(e);
                break;
            case DocumentExtractionFailed e:
                HandleDocumentFailed(e);
                break;
            case DocumentExtractionCancelled e:
                HandleDocumentCancelled(e);
                break;
            case PageRasterizationFailed e:
                HandlePageRasterizationFailed(e);
                break;
            case OcrProcessingStarted e:
                HandleOcrProcessingStarted(e);
                break;
            case OcrProcessingFailed e:
                HandleOcrProcessingFailed(e);
                break;
            case EmptyPageDetected e:
                HandleEmptyPageDetected(e);
                break;
            case TempImageSaved e:
                HandleTempImageSaved(e);
                break;
            case TempFilesCleanedUp e:
                HandleTempFilesCleanedUp(e);
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

    private void HandleBatchFailed(BatchExtractionFailed e)
    {
        IsExtracting = false;
        Status = $"❌ Batch extraction failed: {e.ErrorMessage} ({e.FilesProcessedBeforeFailure} files processed)";
        _logger.Error($"Batch extraction failed. Exception: {e.ExceptionType}, Message: {e.ErrorMessage}, Files processed: {e.FilesProcessedBeforeFailure}");
    }

    private void HandleBatchCancelled(BatchExtractionCancelled e)
    {
        IsExtracting = false;
        Status = $"⚠️ Batch extraction cancelled: {e.Reason} ({e.FilesProcessedBeforeCancellation} files processed)";
        _logger.Info($"Batch extraction cancelled. Reason: {e.Reason}, Files processed: {e.FilesProcessedBeforeCancellation}");
    }

    private void HandleDocumentFailed(DocumentExtractionFailed e)
    {
        var fileName = Path.GetFileName(e.FilePath);
        var pageInfo = e.PageNumberWhereFailed.HasValue ? $" at page {e.PageNumberWhereFailed.Value}" : "";
        Status = $"❌ Document failed: {fileName}{pageInfo} - {e.ErrorMessage}";

        var docGroup = PdfDocuments.FirstOrDefault(d => d.FilePath == e.FilePath);
        if (docGroup != null)
        {
            docGroup.Status = DocumentStatus.Failed;
            docGroup.ErrorMessage = e.ErrorMessage;
        }

        _logger.Error($"Document extraction failed: {e.FilePath}, Exception: {e.ExceptionType}, Message: {e.ErrorMessage}, Page: {e.PageNumberWhereFailed}");
    }

    private void HandleDocumentCancelled(DocumentExtractionCancelled e)
    {
        var fileName = Path.GetFileName(e.FilePath);
        Status = $"⚠️ Document cancelled: {fileName} ({e.PagesProcessedBeforeCancellation} pages processed)";

        var docGroup = PdfDocuments.FirstOrDefault(d => d.FilePath == e.FilePath);
        if (docGroup != null)
        {
            docGroup.Status = DocumentStatus.Cancelled;
        }

        _logger.Info($"Document extraction cancelled: {e.FilePath}, Pages processed: {e.PagesProcessedBeforeCancellation}");
    }

    private void HandlePageRasterizationFailed(PageRasterizationFailed e)
    {
        var page = FindPage(e.FilePath, e.PageNumber);
        if (page != null)
        {
            page.Status = PageStatus.Failed;
        }

        _logger.Error($"Page rasterization failed: {e.FilePath}, Page {e.PageNumber}, Exception: {e.ExceptionType}, Message: {e.ErrorMessage}");
    }

    private void HandleOcrProcessingFailed(OcrProcessingFailed e)
    {
        var page = FindPage(e.FilePath, e.PageNumber);
        if (page != null)
        {
            page.Status = PageStatus.Failed;
        }

        _logger.Error($"OCR processing failed: {e.FilePath}, Page {e.PageNumber}, Exception: {e.ExceptionType}, Message: {e.ErrorMessage}");
    }

    private void HandleOcrProcessingStarted(OcrProcessingStarted e)
    {
        var page = FindPage(e.FilePath, e.PageNumber);
        if (page != null)
        {
            page.Status = PageStatus.OcrProcessing;
        }

        Status = $"OCR processing page {e.PageNumber} (Model: {e.VisionModelName})";
        _logger.Info($"OCR processing started: {e.FilePath}, Page {e.PageNumber}, Model: {e.VisionModelName}");
    }

    private void HandleEmptyPageDetected(EmptyPageDetected e)
    {
        var page = FindPage(e.FilePath, e.PageNumber);
        if (page != null)
        {
            page.Status = PageStatus.Completed;
        }

        _logger.Info($"Empty page detected: {e.FilePath}, Page {e.PageNumber}");
    }

    private void HandleTempImageSaved(TempImageSaved e)
    {
        _logger.Debug($"Temp image saved: {e.TempImagePath}, Size: {e.ImageSizeBytes} bytes, Page {e.PageNumber}");
    }

    private void HandleTempFilesCleanedUp(TempFilesCleanedUp e)
    {
        _logger.Debug($"Temp files cleaned up: {e.TotalFilesDeleted} files deleted");
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
        if (IsExtracting || string.IsNullOrWhiteSpace(InputFolderPath) || string.IsNullOrWhiteSpace(OutputFolderPath))
            return false;

        return SelectedMethod switch
        {
            TextExtractionMethod.PdfPig => PdfPigConfig.IsValid(),
            TextExtractionMethod.LMStudio => LMStudioConfig.IsValid(),
            TextExtractionMethod.OpenAI => OpenAIConfig.IsValid(),
            _ => false
        };
    }

    private async Task StartExtractionAsync()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Status = "Starting extraction...";
            _logger.Info($"Starting {SelectedMethod} extraction");

            switch (SelectedMethod)
            {
                case TextExtractionMethod.PdfPig:
                    var pdfPigParams = new PdfPigParameters
                    {
                        PdfFolderPath = InputFolderPath,
                        OutputFolderPath = OutputFolderPath
                    };
                    await _extractorLib.ExtractWithPdfPigAsync(pdfPigParams, _cancellationTokenSource.Token);
                    break;

                case TextExtractionMethod.LMStudio:
                    var lmStudioParams = new LMStudioParameters
                    {
                        PdfFolderPath = InputFolderPath,
                        OutputFolderPath = OutputFolderPath,
                        LMStudioUrl = LMStudioConfig.LMStudioUrl,
                        VisionModelName = LMStudioConfig.VisionModelName,
                        RasterizationDpi = LMStudioConfig.Dpi,
                        MaxTokens = LMStudioConfig.MaxTokens,
                        ExtractionPrompt = LMStudioConfig.ExtractionPrompt
                    };
                    await _extractorLib.ExtractWithLMStudioAsync(lmStudioParams, _cancellationTokenSource.Token);
                    break;

                case TextExtractionMethod.OpenAI:
                    var openAIParams = new OpenAIParameters
                    {
                        PdfFolderPath = InputFolderPath,
                        OutputFolderPath = OutputFolderPath,
                        ApiKey = OpenAIConfig.OpenAIApiKey,
                        VisionModelName = OpenAIConfig.OpenAIModelName,
                        RasterizationDpi = OpenAIConfig.OpenAIDpi,
                        MaxTokens = OpenAIConfig.MaxTokens,
                        ExtractionPrompt = OpenAIConfig.ExtractionPrompt
                    };
                    await _extractorLib.ExtractWithOpenAIAsync(openAIParams, _cancellationTokenSource.Token);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported extraction method: {SelectedMethod}");
            }

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

    private void OnChildConfigChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When child configuration changes, re-evaluate command can-execute state
        (StartExtractionCommand as AsyncCommand)?.RaiseCanExecuteChanged();
    }

    #endregion

    public void Dispose()
    {
        // Unsubscribe from child ViewModel events
        PdfPigConfig.PropertyChanged -= OnChildConfigChanged;
        LMStudioConfig.PropertyChanged -= OnChildConfigChanged;
        OpenAIConfig.PropertyChanged -= OnChildConfigChanged;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _disposables.Dispose();
        (_extractorLib as IDisposable)?.Dispose();
    }
}
