using DevExpress.Mvvm;

namespace PdfTextExtractor.Wpf.ViewModels;

/// <summary>
/// ViewModel for PdfPig configuration.
/// PdfPig performs native PDF text extraction and requires no additional configuration.
/// </summary>
public class PdfPigConfigViewModel : ViewModelBase
{
    /// <summary>
    /// PdfPig is always valid as it requires no configuration.
    /// </summary>
    public bool IsValid() => true;

    /// <summary>
    /// Description of the PdfPig extraction method.
    /// </summary>
    public string Description =>
        "PdfPig extracts text directly from PDF files without OCR. " +
        "Best for PDFs with native text content (not scanned images). " +
        "Fast, free, and requires no API keys or external services.";
}
