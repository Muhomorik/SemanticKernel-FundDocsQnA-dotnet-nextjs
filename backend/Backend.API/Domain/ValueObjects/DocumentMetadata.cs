namespace Backend.API.Domain.ValueObjects;

/// <summary>
/// Value object representing document source metadata.
/// Immutable and equality by value.
/// </summary>
public record DocumentMetadata
{
    public string Source { get; init; }
    public int Page { get; init; }

    public DocumentMetadata(string source, int page)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 1");

        Source = source;
        Page = page;
    }
}
