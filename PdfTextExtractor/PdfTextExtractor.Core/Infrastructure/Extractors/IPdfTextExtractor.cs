using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.Extractors;

/// <summary>
/// Interface for PDF text extractors.
/// </summary>
public interface IPdfTextExtractor
{
    TextExtractionMethod Method { get; }

    Task<IEnumerable<DocumentChunk>> ExtractAsync(
        string filePath,
        IEventPublisher eventPublisher,
        Guid correlationId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
