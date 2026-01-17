using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Domain.Entities;

/// <summary>
/// Page entity containing text chunks.
/// </summary>
public class Page
{
    public Guid PageId { get; private set; }
    public PageNumber PageNumber { get; private set; }
    public List<TextChunk> Chunks { get; private set; } = new();
    public int ExtractedTextLength { get; private set; }
    public bool IsEmpty { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private Page() { } // EF Core constructor

    public static Page Create(PageNumber pageNumber)
    {
        return new Page
        {
            PageId = Guid.NewGuid(),
            PageNumber = pageNumber,
            IsEmpty = false
        };
    }

    public void AddChunk(TextChunk chunk)
    {
        if (chunk == null)
            throw new ArgumentNullException(nameof(chunk));

        Chunks.Add(chunk);
        ExtractedTextLength += chunk.Content.Length;
    }

    public void MarkAsEmpty()
    {
        IsEmpty = true;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
