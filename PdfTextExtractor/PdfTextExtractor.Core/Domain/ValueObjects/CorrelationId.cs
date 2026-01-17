namespace PdfTextExtractor.Core.Domain.ValueObjects;

/// <summary>
/// Strongly-typed Correlation ID for single document tracking.
/// </summary>
public sealed record CorrelationId
{
    public Guid Value { get; }

    private CorrelationId(Guid value)
    {
        Value = value;
    }

    public static CorrelationId Create() => new(Guid.NewGuid());
    public static CorrelationId FromGuid(Guid guid) => new(guid);
    public static implicit operator Guid(CorrelationId id) => id.Value;
}
