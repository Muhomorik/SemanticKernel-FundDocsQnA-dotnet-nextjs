using System;
using DevExpress.Mvvm;

namespace PdfTextExtractor.Wpf.ViewModels;

/// <summary>
/// ViewModel representing a single PDF page preview with extraction status.
/// </summary>
public sealed class PagePreviewViewModel : ViewModelBase
{
    // Identity
    private string _pdfFileName = "";
    private string _filePath = "";
    private int _pageNumber;
    private Guid _correlationId;

    // State
    private string? _imagePath;
    private PageStatus _status = PageStatus.NotStarted;

    /// <summary>
    /// Gets or sets the PDF file name.
    /// </summary>
    public string PdfFileName
    {
        get => _pdfFileName;
        set => SetProperty(ref _pdfFileName, value, nameof(PdfFileName));
    }

    /// <summary>
    /// Gets or sets the full file path.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value, nameof(FilePath));
    }

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => SetProperty(ref _pageNumber, value, nameof(PageNumber));
    }

    /// <summary>
    /// Gets or sets the correlation ID for tracking this page's extraction.
    /// </summary>
    public Guid CorrelationId
    {
        get => _correlationId;
        set => SetProperty(ref _correlationId, value, nameof(CorrelationId));
    }

    /// <summary>
    /// Gets or sets the path to the rasterized page image.
    /// </summary>
    public string? ImagePath
    {
        get => _imagePath;
        set => SetProperty(ref _imagePath, value, nameof(ImagePath));
    }

    /// <summary>
    /// Gets or sets the current status of this page's extraction.
    /// </summary>
    public PageStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value, nameof(Status)))
            {
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(StatusIcon));
            }
        }
    }

    /// <summary>
    /// Gets the human-readable status text.
    /// </summary>
    public string StatusText => Status switch
    {
        PageStatus.Completed => "Completed",
        PageStatus.Extracting => "Extracting...",
        PageStatus.Rasterizing => "Rasterizing...",
        PageStatus.Failed => "Failed",
        _ => "Waiting"
    };

    /// <summary>
    /// Gets the status icon emoji.
    /// </summary>
    public string StatusIcon => Status switch
    {
        PageStatus.Completed => "✓",
        PageStatus.Extracting => "⟳",
        PageStatus.Rasterizing => "⚙",
        PageStatus.Failed => "✗",
        _ => "○"
    };
}

/// <summary>
/// Represents the extraction status of a PDF page.
/// </summary>
public enum PageStatus
{
    /// <summary>Page has not started processing yet.</summary>
    NotStarted,

    /// <summary>Page is being rasterized to an image.</summary>
    Rasterizing,

    /// <summary>Text extraction is in progress.</summary>
    Extracting,

    /// <summary>Extraction completed successfully.</summary>
    Completed,

    /// <summary>Extraction failed.</summary>
    Failed
}
