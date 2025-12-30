namespace Backend.API.Domain.ValueObjects;

/// <summary>
/// Value object wrapping embedding vector with validation.
/// </summary>
public record EmbeddingVector
{
    public float[] Values { get; init; }
    public int Dimensions => Values.Length;

    public EmbeddingVector(float[] values)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Vector cannot be empty", nameof(values));

        Values = values;
    }
}
