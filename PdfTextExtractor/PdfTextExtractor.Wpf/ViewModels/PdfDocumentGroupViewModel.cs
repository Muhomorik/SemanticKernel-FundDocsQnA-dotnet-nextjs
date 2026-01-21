using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm;

namespace PdfTextExtractor.Wpf.ViewModels;

/// <summary>
/// ViewModel representing a PDF document group containing multiple page previews.
/// </summary>
public sealed class PdfDocumentGroupViewModel : ViewModelBase
{
    private string _fileName = "";
    private string _filePath = "";
    private ObservableCollection<PagePreviewViewModel> _pages = new();
    private bool _isExpanded = true;
    private DocumentStatus _status = DocumentStatus.Processing;
    private string _errorMessage = "";

    /// <summary>
    /// Gets or sets the PDF file name (without path).
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value, nameof(FileName));
    }

    /// <summary>
    /// Gets or sets the full file path to the PDF.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value, nameof(FilePath));
    }

    /// <summary>
    /// Gets or sets the collection of page preview ViewModels.
    /// </summary>
    public ObservableCollection<PagePreviewViewModel> Pages
    {
        get => _pages;
        set
        {
            if (SetProperty(ref _pages, value, nameof(Pages)))
            {
                RaisePropertyChanged(nameof(TotalPages));
                RaisePropertyChanged(nameof(CompletedPages));
                RaisePropertyChanged(nameof(ProgressText));
                RaisePropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the expander is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value, nameof(IsExpanded));
    }

    /// <summary>
    /// Gets or sets the document extraction status.
    /// </summary>
    public DocumentStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value, nameof(Status));
    }

    /// <summary>
    /// Gets or sets the error message if extraction failed.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value, nameof(ErrorMessage));
    }

    /// <summary>
    /// Gets the total number of pages in this document.
    /// </summary>
    public int TotalPages => Pages.Count;

    /// <summary>
    /// Gets the number of completed pages.
    /// </summary>
    public int CompletedPages => Pages.Count(p => p.Status == PageStatus.Completed);

    /// <summary>
    /// Gets the progress text (e.g., "3/10 pages").
    /// </summary>
    public string ProgressText => $"{CompletedPages}/{TotalPages} pages";

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage => TotalPages > 0
        ? (double)CompletedPages / TotalPages * 100
        : 0;
}

/// <summary>
/// Represents the extraction status of a PDF document.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Document is being processed.</summary>
    Processing,

    /// <summary>Document extraction completed successfully.</summary>
    Completed,

    /// <summary>Document extraction failed.</summary>
    Failed,

    /// <summary>Document extraction was cancelled.</summary>
    Cancelled
}
