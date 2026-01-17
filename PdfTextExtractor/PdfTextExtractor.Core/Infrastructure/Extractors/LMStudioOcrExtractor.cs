using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.Extractors;

/// <summary>
/// LM Studio OCR-based text extractor (stub - to be implemented in Phase 6).
/// </summary>
public class LMStudioOcrExtractor : IPdfTextExtractor
{
    public TextExtractionMethod Method => TextExtractionMethod.LMStudio;

    public Task<IEnumerable<DocumentChunk>> ExtractAsync(
        string filePath,
        IEventPublisher eventPublisher,
        Guid correlationId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("LM Studio OCR extraction not yet implemented. Coming in Phase 6.");
    }
}
