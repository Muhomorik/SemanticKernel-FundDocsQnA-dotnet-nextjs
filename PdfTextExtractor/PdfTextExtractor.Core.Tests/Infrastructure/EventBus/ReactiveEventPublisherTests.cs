using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.Events.Document;
using PdfTextExtractor.Core.Infrastructure.EventBus;
using PdfTextExtractor.Core.Tests.AutoFixture;
using System.Reactive.Linq;

namespace PdfTextExtractor.Core.Tests.Infrastructure.EventBus;

[TestFixture]
[TestOf(typeof(ReactiveEventPublisher))]
public class ReactiveEventPublisherTests
{
    private IFixture _fixture;
    private ReactiveEventPublisher _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _sut = new ReactiveEventPublisher();
    }

    [TearDown]
    public void TearDown()
    {
        _sut?.Dispose();
    }

    [Test]
    public void Publish_ValidEvent_EmitsEventToObservers()
    {
        // Arrange
        var receivedEvent = default(DocumentExtractionStarted);
        _sut.Events.OfType<DocumentExtractionStarted>()
            .Subscribe(e => receivedEvent = e);

        var @event = new DocumentExtractionStarted
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            ExtractorName = "PdfPig",
            FilePath = "test.pdf",
            FileName = "test.pdf",
            FileSizeBytes = 1000
        };

        // Act
        _sut.Publish(@event);

        // Assert
        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent.FilePath, Is.EqualTo("test.pdf"));
    }

    [Test]
    public async Task PublishAsync_ValidEvent_EmitsEventToObservers()
    {
        // Arrange
        var receivedEvent = default(DocumentExtractionStarted);
        _sut.Events.OfType<DocumentExtractionStarted>()
            .Subscribe(e => receivedEvent = e);

        var @event = new DocumentExtractionStarted
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            ExtractorName = "PdfPig",
            FilePath = "test.pdf",
            FileName = "test.pdf",
            FileSizeBytes = 1000
        };

        // Act
        await _sut.PublishAsync(@event);

        // Assert
        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent.FilePath, Is.EqualTo("test.pdf"));
    }

    [Test]
    public async Task PublishAsync_CancellationRequested_ReturnsCancelledTask()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var @event = new DocumentExtractionStarted
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            ExtractorName = "PdfPig",
            FilePath = "test.pdf",
            FileName = "test.pdf",
            FileSizeBytes = 1000
        };

        // Act
        var task = _sut.PublishAsync(@event, cts.Token);

        // Assert
        Assert.That(task.IsCanceled, Is.True);
        await Task.CompletedTask;
    }

    [Test]
    public void Events_MultipleSubscribers_AllReceiveEvents()
    {
        // Arrange
        var received1 = default(DocumentExtractionStarted);
        var received2 = default(DocumentExtractionStarted);

        _sut.Events.OfType<DocumentExtractionStarted>().Subscribe(e => received1 = e);
        _sut.Events.OfType<DocumentExtractionStarted>().Subscribe(e => received2 = e);

        var @event = new DocumentExtractionStarted
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            ExtractorName = "PdfPig",
            FilePath = "test.pdf",
            FileName = "test.pdf",
            FileSizeBytes = 1000
        };

        // Act
        _sut.Publish(@event);

        // Assert
        Assert.That(received1, Is.Not.Null);
        Assert.That(received2, Is.Not.Null);
        Assert.That(received1.FilePath, Is.EqualTo(received2.FilePath));
    }
}
