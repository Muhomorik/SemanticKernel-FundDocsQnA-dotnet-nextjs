using DevExpress.Mvvm;

namespace PdfTextExtractor.Wpf.ViewModels;

/// <summary>
/// ViewModel for PdfPig configuration.
/// PdfPig performs native PDF text extraction and requires no additional configuration.
/// </summary>
public sealed class PdfPigConfigViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfPigConfigViewModel"/> class.
    /// </summary>
    public PdfPigConfigViewModel()
    {
        // No configuration required for PdfPig - it uses native PDF text extraction
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>Always returns true as PdfPig requires no configuration.</returns>
    public bool IsValid() => true;

    /// <summary>
    /// Gets the description of the PdfPig extraction method.
    /// </summary>
    public string Description =>
        "PdfPig extracts text directly from PDF files without OCR. " +
        "Best for PDFs with native text content (not scanned images). " +
        "Fast, free, and requires no API keys or external services.";
}
