using Backend.API.Domain.ValueObjects;

namespace Backend.API.Domain.Services;

/// <summary>
/// Pure domain service for calculating cosine similarity.
/// No I/O dependencies - pure computation.
/// </summary>
/// <remarks>
/// Semantic Kernel provides built-in vector search capabilities through the Vector Store
/// abstractions (VectorStore, VectorStoreCollection). 
/// 
/// Legacy alternatives (IMemoryStore, SemanticTextMemory) are deprecated.
/// 
/// This manual implementation is used for simplicity,
/// explicit control, and working with pre-computed embeddings from JSON.
/// </remarks>
public static class CosineSimilarityCalculator
{
    public static float Calculate(EmbeddingVector vector1, EmbeddingVector vector2)
    {
        if (vector1.Dimensions != vector2.Dimensions)
            throw new ArgumentException("Vectors must have the same dimensions");

        var v1 = vector1.Values;
        var v2 = vector2.Values;

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            magnitude1 += v1[i] * v1[i];
            magnitude2 += v2[i] * v2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        // Clamp result to [0, 1] to handle floating-point precision errors
        var similarity = dotProduct / (magnitude1 * magnitude2);
        return Math.Clamp(similarity, 0f, 1f);
    }
}
