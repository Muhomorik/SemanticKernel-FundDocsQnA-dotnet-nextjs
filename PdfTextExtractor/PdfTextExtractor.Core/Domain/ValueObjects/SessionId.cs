namespace PdfTextExtractor.Core.Domain.ValueObjects;

/// <summary>
/// Strongly-typed Session ID for batch operations.
/// </summary>
public sealed record SessionId
{
    public Guid Value { get; }

    private SessionId(Guid value)
    {
        Value = value;
    }

    public static SessionId Create() => new(Guid.NewGuid());
    public static SessionId FromGuid(Guid guid) => new(guid);
    public static implicit operator Guid(SessionId id) => id.Value;
}
