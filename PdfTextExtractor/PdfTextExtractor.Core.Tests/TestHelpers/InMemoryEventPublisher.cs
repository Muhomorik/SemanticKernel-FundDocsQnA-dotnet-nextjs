using System.Collections.Concurrent;
using PdfTextExtractor.Core.Domain.Events;

namespace PdfTextExtractor.Core.Tests.TestHelpers;

/// <summary>
/// In-memory event publisher for testing purposes.
/// Stores all published events in memory for verification.
/// </summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ConcurrentBag<PdfExtractionEventBase> _publishedEvents = new();

    public void Publish<TEvent>(TEvent @event) where TEvent : PdfExtractionEventBase
    {
        _publishedEvents.Add(@event);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : PdfExtractionEventBase
    {
        _publishedEvents.Add(@event);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all events that have been published.
    /// </summary>
    public IEnumerable<PdfExtractionEventBase> GetPublishedEvents()
    {
        return _publishedEvents.ToList();
    }

    /// <summary>
    /// Gets all events of a specific type.
    /// </summary>
    public IEnumerable<TEvent> GetEventsOfType<TEvent>() where TEvent : PdfExtractionEventBase
    {
        return _publishedEvents.OfType<TEvent>().ToList();
    }

    /// <summary>
    /// Clears all published events.
    /// </summary>
    public void Clear()
    {
        _publishedEvents.Clear();
    }

    /// <summary>
    /// Gets the count of published events.
    /// </summary>
    public int Count => _publishedEvents.Count;
}
