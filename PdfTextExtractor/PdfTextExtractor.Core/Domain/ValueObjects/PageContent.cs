namespace PdfTextExtractor.Core.Domain.ValueObjects;

/// <summary>
/// Immutable text content for a page.
/// </summary>
public sealed record PageContent
{
    public string Value { get; }
    public int Length => Value.Length;

    private PageContent(string value)
    {
        Value = value;
    }

    public static PageContent Create(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Page content cannot be empty.", nameof(content));

        return new PageContent(content);
    }

    public string Preview(int maxLength = 100)
    {
        return Value.Length <= maxLength
            ? Value
            : Value.Substring(0, maxLength) + "...";
    }
}
