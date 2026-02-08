using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Domain.Aggregates;
using PdfTextExtractor.Core.Domain.Events.Batch;
using PdfTextExtractor.Core.Domain.Events.Document;
using PdfTextExtractor.Core.Domain.ValueObjects;
using PdfTextExtractor.Core.Tests.AutoFixture;

namespace PdfTextExtractor.Core.Tests.Domain.Aggregates;

[TestFixture]
public class ExtractionSessionTests
{
    private IFixture _fixture;
    private ExtractorType _extractorType;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new PdfTextExtractorCustomization());

        _extractorType = _fixture.Create<ExtractorType>();
    }

    [Test]
    public void Create_ValidExtractorType_ReturnsSession()
    {
        // Arrange & Act
        var session = ExtractionSession.Create(_extractorType);

        // Assert
        Assert.That(session, Is.Not.Null);
        Assert.That(session.SessionId, Is.Not.Null);
        Assert.That(session.ExtractorType, Is.EqualTo(_extractorType));
    }

    [Test]
    public void Create_ValidExtractorType_RaisesBatchExtractionStartedEvent()
    {
        // Arrange & Act
        var session = ExtractionSession.Create(_extractorType);

        // Assert
        Assert.That(session.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(session.DomainEvents.First(), Is.TypeOf<BatchExtractionStarted>());
    }

    [Test]
    public void AddDocument_ValidParameters_AddsDocumentToCollection()
    {
        // Arrange
        var session = ExtractionSession.Create(_extractorType);
        var filePath = _fixture.Create<FilePath>();
        var fileSizeBytes = _fixture.Create<long>();
        session.ClearDomainEvents(); // Clear creation event for this test

        // Act
        var document = session.AddDocument(filePath, fileSizeBytes);

        // Assert
        Assert.That(session.Documents, Contains.Item(document));
        Assert.That(session.TotalDocuments, Is.EqualTo(1));
    }

    [Test]
    public void AddDocument_ValidParameters_RaisesDocumentExtractionStartedEvent()
    {
        // Arrange
        var session = ExtractionSession.Create(_extractorType);
        var filePath = _fixture.Create<FilePath>();
        var fileSizeBytes = 5000L;
        session.ClearDomainEvents();

        // Act
        session.AddDocument(filePath, fileSizeBytes);

        // Assert
        Assert.That(session.DomainEvents, Has.Count.EqualTo(1));
        var domainEvent = session.DomainEvents.First() as DocumentExtractionStarted;
        Assert.That(domainEvent, Is.Not.Null);
        Assert.That(domainEvent.FilePath, Is.EqualTo(filePath.Value));
        Assert.That(domainEvent.FileSizeBytes, Is.EqualTo(fileSizeBytes));
    }

    [Test]
    public void MarkAsCompleted_ValidSession_SetsCompletedAt()
    {
        // Arrange
        var session = ExtractionSession.Create(_extractorType);

        // Act
        session.MarkAsCompleted();

        // Assert
        Assert.That(session.IsCompleted, Is.True);
        Assert.That(session.CompletedAt, Is.Not.Null);
    }

    [Test]
    public void MarkAsCompleted_ValidSession_RaisesBatchExtractionCompletedEvent()
    {
        // Arrange
        var session = ExtractionSession.Create(_extractorType);
        session.ClearDomainEvents();

        // Act
        session.MarkAsCompleted();

        // Assert
        Assert.That(session.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(session.DomainEvents.First(), Is.TypeOf<BatchExtractionCompleted>());
    }

    [Test]
    public void MarkAsCancelled_ValidSession_SetsCancelledFlag()
    {
        // Arrange
        var session = ExtractionSession.Create(_extractorType);

        // Act
        session.MarkAsCancelled();

        // Assert
        Assert.That(session.IsCancelled, Is.True);
    }

    [Test]
    public void MarkAsCancelled_ValidSession_RaisesBatchExtractionCancelledEvent()
    {
        // Arrange
        var session = ExtractionSession.Create(_extractorType);
        session.ClearDomainEvents();

        // Act
        session.MarkAsCancelled();

        // Assert
        Assert.That(session.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(session.DomainEvents.First(), Is.TypeOf<BatchExtractionCancelled>());
    }

    [Test]
    public void ClearDomainEvents_AfterOperations_RemovesAllEvents()
    {
        // Arrange
        var session = ExtractionSession.Create(_extractorType);
        Assert.That(session.DomainEvents, Is.Not.Empty);

        // Act
        session.ClearDomainEvents();

        // Assert
        Assert.That(session.DomainEvents, Is.Empty);
    }

    [Test]
    public void CompletedDocuments_MixedCompletionStates_ReturnsCorrectCount()
    {
        // Arrange
        var session = ExtractionSession.Create(_extractorType);
        var doc1 = session.AddDocument(_fixture.Create<FilePath>(), 1000);
        var doc2 = session.AddDocument(_fixture.Create<FilePath>(), 2000);
        var doc3 = session.AddDocument(_fixture.Create<FilePath>(), 3000);

        doc1.MarkAsCompleted();
        doc3.MarkAsCompleted();

        // Act
        var completedCount = session.CompletedDocuments;

        // Assert
        Assert.That(completedCount, Is.EqualTo(2));
    }
}
