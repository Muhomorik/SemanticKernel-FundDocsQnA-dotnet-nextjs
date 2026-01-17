using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.Extractors;

/// <summary>
/// Ollama OCR-based text extractor (stub - planned for future implementation).
/// </summary>
public class OllamaOcrExtractor : IPdfTextExtractor
{
    public TextExtractionMethod Method => TextExtractionMethod.Ollama;

    public Task<IEnumerable<DocumentChunk>> ExtractAsync(
        string filePath,
        IEventPublisher eventPublisher,
        Guid correlationId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Ollama OCR extraction planned for future implementation.");
    }
}
