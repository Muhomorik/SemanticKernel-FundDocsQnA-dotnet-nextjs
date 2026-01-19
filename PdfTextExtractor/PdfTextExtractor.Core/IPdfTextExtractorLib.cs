using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core;

/// <summary>
/// Public API interface for PdfTextExtractor library.
/// </summary>
public interface IPdfTextExtractorLib
{
    /// <summary>
    /// Observable stream of all extraction events.
    /// </summary>
    IObservable<PdfExtractionEventBase> Events { get; }

    /// <summary>
    /// Get all PDF file paths from the specified folder.
    /// </summary>
    string[] GetPdfFiles(string folderPath);

    /// <summary>
    /// Get all text file paths from the specified folder.
    /// </summary>
    string[] GetTextFiles(string folderPath);

    /// <summary>
    /// Extract text using PdfPig (native PDF text extraction).
    /// </summary>
    Task<ExtractionResult> ExtractWithPdfPigAsync(
        PdfPigParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text using LM Studio vision models (OCR).
    /// </summary>
    Task<ExtractionResult> ExtractWithLMStudioAsync(
        LMStudioParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text using Ollama vision models (OCR - planned).
    /// </summary>
    Task<ExtractionResult> ExtractWithOllamaAsync(
        OllamaParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text using OpenAI vision models (OCR).
    /// </summary>
    Task<ExtractionResult> ExtractWithOpenAIAsync(
        OpenAIParameters parameters,
        CancellationToken cancellationToken = default);
}
