using PdfTextExtractor.Core.Domain.Entities;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Batch;
using PdfTextExtractor.Core.Domain.Events.Document;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Domain.Aggregates;

/// <summary>
/// Aggregate root for a batch extraction session.
/// Enforces invariants and controls access to Documents.
/// </summary>
public class ExtractionSession
{
    public SessionId SessionId { get; private set; }
    public ExtractorType ExtractorType { get; private set; }
    public List<Document> Documents { get; private set; } = new();
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsCompleted => CompletedAt.HasValue;
    public bool IsCancelled { get; private set; }

    private readonly List<PdfExtractionEventBase> _domainEvents = new();
    public IReadOnlyCollection<PdfExtractionEventBase> DomainEvents => _domainEvents.AsReadOnly();

    private ExtractionSession() { } // EF Core constructor

    public static ExtractionSession Create(ExtractorType extractorType)
    {
        var session = new ExtractionSession
        {
            SessionId = SessionId.Create(),
            ExtractorType = extractorType,
            StartedAt = DateTimeOffset.UtcNow
        };

        // Raise domain event
        session.RaiseEvent(new BatchExtractionStarted
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = session.SessionId,
            ExtractorName = extractorType.Value,
            FilePaths = Array.Empty<string>(),
            TotalFiles = 0
        });

        return session;
    }

    public Document AddDocument(FilePath filePath, long fileSizeBytes)
    {
        var correlationId = CorrelationId.Create();
        var document = Document.Create(filePath, fileSizeBytes, correlationId);
        Documents.Add(document);

        // Raise domain event
        RaiseEvent(new DocumentExtractionStarted
        {
            CorrelationId = correlationId,
            SessionId = SessionId,
            ExtractorName = ExtractorType.Value,
            FilePath = filePath.Value,
            FileName = filePath.FileName,
            FileSizeBytes = fileSizeBytes
        });

        return document;
    }

    public void MarkAsCompleted()
    {
        CompletedAt = DateTimeOffset.UtcNow;

        // Raise domain event
        RaiseEvent(new BatchExtractionCompleted
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = SessionId,
            ExtractorName = ExtractorType.Value,
            OutputFilePaths = Array.Empty<string>(),
            TotalFilesProcessed = Documents.Count(d => d.IsCompleted),
            TotalDuration = CompletedAt.Value - StartedAt
        });
    }

    public void MarkAsCancelled()
    {
        IsCancelled = true;

        // Raise domain event
        RaiseEvent(new BatchExtractionCancelled
        {
            CorrelationId = Guid.NewGuid(),
            SessionId = SessionId,
            ExtractorName = ExtractorType.Value,
            Reason = "User cancelled",
            FilesProcessedBeforeCancellation = Documents.Count(d => d.IsCompleted)
        });
    }

    private void RaiseEvent(PdfExtractionEventBase domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public int TotalDocuments => Documents.Count;
    public int CompletedDocuments => Documents.Count(d => d.IsCompleted);
}
