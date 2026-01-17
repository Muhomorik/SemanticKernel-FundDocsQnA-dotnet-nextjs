namespace PdfTextExtractor.Core.Domain.ValueObjects;

/// <summary>
/// Immutable text content for a chunk.
/// </summary>
public sealed record ChunkContent
{
    public string Value { get; }
    public int Length => Value.Length;

    private ChunkContent(string value)
    {
        Value = value;
    }

    public static ChunkContent Create(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Chunk content cannot be empty.", nameof(content));

        return new ChunkContent(content);
    }

    public string Preview(int maxLength = 100)
    {
        return Value.Length <= maxLength
            ? Value
            : Value.Substring(0, maxLength) + "...";
    }
}
