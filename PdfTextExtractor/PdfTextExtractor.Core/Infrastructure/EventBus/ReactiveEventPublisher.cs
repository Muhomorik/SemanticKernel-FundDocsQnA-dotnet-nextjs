using System.Reactive.Subjects;
using PdfTextExtractor.Core.Domain.Events;

namespace PdfTextExtractor.Core.Infrastructure.EventBus;

/// <summary>
/// Reactive event publisher using Rx.NET Subject.
/// </summary>
public class ReactiveEventPublisher : IEventPublisher, IDisposable
{
    private readonly Subject<PdfExtractionEventBase> _eventStream;

    public ReactiveEventPublisher()
    {
        _eventStream = new Subject<PdfExtractionEventBase>();
    }

    public IObservable<PdfExtractionEventBase> Events => _eventStream;

    public void Publish<TEvent>(TEvent @event) where TEvent : PdfExtractionEventBase
    {
        _eventStream.OnNext(@event);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : PdfExtractionEventBase
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        _eventStream.OnNext(@event);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _eventStream?.Dispose();
    }
}
