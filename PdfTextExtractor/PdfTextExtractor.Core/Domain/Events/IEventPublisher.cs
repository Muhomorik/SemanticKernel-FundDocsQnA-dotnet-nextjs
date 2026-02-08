namespace PdfTextExtractor.Core.Domain.Events;

/// <summary>
/// Domain interface for publishing events.
/// </summary>
public interface IEventPublisher
{
    void Publish<TEvent>(TEvent @event) where TEvent : PdfExtractionEventBase;
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : PdfExtractionEventBase;
}
