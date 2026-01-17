using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Domain.Entities;

/// <summary>
/// Document entity representing a PDF file being processed.
/// </summary>
public class Document
{
    public Guid DocumentId { get; private set; }
    public CorrelationId CorrelationId { get; private set; }
    public FilePath FilePath { get; private set; }
    public long FileSizeBytes { get; private set; }
    public List<Page> Pages { get; private set; } = new();
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public TimeSpan? Duration => CompletedAt - StartedAt;
    public bool IsCompleted => CompletedAt.HasValue;

    private Document() { } // EF Core constructor

    public static Document Create(FilePath filePath, long fileSizeBytes, CorrelationId correlationId)
    {
        return new Document
        {
            DocumentId = Guid.NewGuid(),
            CorrelationId = correlationId,
            FilePath = filePath,
            FileSizeBytes = fileSizeBytes,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    public Page AddPage(PageNumber pageNumber)
    {
        var page = Page.Create(pageNumber);
        Pages.Add(page);
        return page;
    }

    public void MarkAsCompleted()
    {
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public int TotalPages => Pages.Count;
    public int TotalChunks => Pages.Sum(p => p.Chunks.Count);
}
