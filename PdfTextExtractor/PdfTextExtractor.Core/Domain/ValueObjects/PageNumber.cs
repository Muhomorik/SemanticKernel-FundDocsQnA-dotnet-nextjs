namespace PdfTextExtractor.Core.Domain.ValueObjects;

/// <summary>
/// Immutable, validated 1-based page number.
/// </summary>
public sealed record PageNumber
{
    public int Value { get; }

    private PageNumber(int value)
    {
        Value = value;
    }

    public static PageNumber Create(int pageNumber)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be >= 1.", nameof(pageNumber));

        return new PageNumber(pageNumber);
    }

    public static implicit operator int(PageNumber pageNumber) => pageNumber.Value;
}
