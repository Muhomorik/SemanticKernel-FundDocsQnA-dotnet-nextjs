namespace Backend.API.Domain.Models;

/// <summary>
/// Domain model for search results with similarity score.
/// </summary>
public class SearchResult
{
    public DocumentChunk Chunk { get; init; }
    public float SimilarityScore { get; init; }

    public SearchResult(DocumentChunk chunk, float similarityScore)
    {
        Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));

        if (similarityScore < 0 || similarityScore > 1)
            throw new ArgumentOutOfRangeException(nameof(similarityScore),
                "Similarity score must be between 0 and 1");

        SimilarityScore = similarityScore;
    }
}
