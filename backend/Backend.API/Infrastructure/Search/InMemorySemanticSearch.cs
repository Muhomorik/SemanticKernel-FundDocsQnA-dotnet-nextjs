using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;
using Backend.API.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Backend.API.Infrastructure.Search;

/// <summary>
/// In-memory semantic search using cosine similarity.
/// Combines repository access, embedding generation, and similarity calculation.
/// </summary>
public class InMemorySemanticSearch : ISemanticSearch
{
    private readonly IDocumentRepository _repository;
    private readonly Domain.Interfaces.IEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<InMemorySemanticSearch> _logger;

    public InMemorySemanticSearch(
        IDocumentRepository repository,
        Domain.Interfaces.IEmbeddingGenerator embeddingGenerator,
        ILogger<InMemorySemanticSearch> logger)
    {
        _repository = repository;
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching for: {Query} (max: {Max})", query, maxResults);

        // Generate query embedding
        var queryVector = await _embeddingGenerator.GenerateEmbeddingAsync(
            query,
            cancellationToken);

        // Get all chunks
        var chunks = await _repository.GetAllChunksAsync();

        // Calculate similarities and rank
        var results = chunks
            .Select(chunk => new SearchResult(
                chunk,
                CosineSimilarityCalculator.Calculate(queryVector, chunk.Vector)))
            .OrderByDescending(r => r.SimilarityScore)
            .Take(maxResults)
            .ToList();

        _logger.LogInformation("Search returned {Count} results", results.Count);
        return results;
    }
}
