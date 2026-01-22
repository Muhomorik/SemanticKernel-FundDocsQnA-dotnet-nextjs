using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Domain.Entities;

/// <summary>
/// Page entity containing extracted text.
/// </summary>
public class Page
{
    public Guid PageId { get; private set; }
    public PageNumber PageNumber { get; private set; }
    public string PageText { get; private set; } = string.Empty;
    public int ExtractedTextLength => PageText.Length;
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

    public void SetPageText(string text)
    {
        PageText = text ?? string.Empty;
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
