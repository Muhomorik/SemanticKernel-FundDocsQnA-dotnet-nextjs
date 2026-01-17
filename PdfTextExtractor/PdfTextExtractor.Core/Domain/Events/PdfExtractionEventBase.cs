namespace PdfTextExtractor.Core.Domain.Events;

/// <summary>
/// Base class for all PDF extraction domain events.
/// </summary>
public abstract class PdfExtractionEventBase
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required Guid CorrelationId { get; init; }
    public required Guid SessionId { get; init; }
    public required string ExtractorName { get; init; }
}
