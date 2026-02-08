namespace PdfTextExtractor.Core.Domain.Events;

/// <summary>
/// Base class for all PDF extraction domain events.
/// </summary>
public abstract class PdfExtractionEventBase
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Unique identifier for tracking a single document through the extraction pipeline. 
    /// Each document has its own CorrelationId.
    /// </summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>
    /// Unique identifier for the batch extraction session. 
    /// All documents and events within the same batch share the same SessionId.
    /// </summary>
    public required Guid SessionId { get; init; }

    public required string ExtractorName { get; init; }
}
