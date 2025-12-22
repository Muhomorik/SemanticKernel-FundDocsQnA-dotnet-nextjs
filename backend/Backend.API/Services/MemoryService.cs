using System.Text.Json;

using Backend.API.Configuration;
using Backend.API.Models;

using Microsoft.SemanticKernel.Embeddings;

namespace Backend.API.Services;

/// <summary>
/// Service for loading document embeddings and performing semantic search using cosine similarity.
/// </summary>
public class MemoryService : IMemoryService
{
    private readonly BackendOptions _options;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly ILogger<MemoryService> _logger;
    private List<EmbeddingRecord> _embeddings = new();
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public MemoryService(
        BackendOptions options,
        ITextEmbeddingGenerationService embeddingService,
        ILogger<MemoryService> logger)
    {
        _options = options;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading embeddings from {Path}", _options.EmbeddingsFilePath);

            if (!File.Exists(_options.EmbeddingsFilePath))
            {
                throw new FileNotFoundException($"Embeddings file not found at: {_options.EmbeddingsFilePath}");
            }

            var json = await File.ReadAllTextAsync(_options.EmbeddingsFilePath, cancellationToken);
            var embeddings = JsonSerializer.Deserialize<List<EmbeddingRecord>>(json);

            if (embeddings == null || embeddings.Count == 0)
            {
                throw new InvalidOperationException("No embeddings found in the file or file is invalid");
            }

            _embeddings = embeddings;
            _logger.LogInformation("Loaded {Count} embeddings", _embeddings.Count);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize memory service");
            throw;
        }
    }

    public async Task<List<EmbeddingRecord>> SearchAsync(string query, int maxResults,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Memory service is not initialized");
        }

        try
        {
            _logger.LogDebug("Searching for: {Query} (max results: {MaxResults})", query, maxResults);

            // Generate embedding for the query
            var queryEmbedding =
                await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

            // Calculate cosine similarity with all embeddings
            var similarities = _embeddings.Select(e => new
                {
                    Embedding = e,
                    Similarity = CosineSimilarity(queryEmbedding.ToArray(), e.Embedding)
                })
                .OrderByDescending(x => x.Similarity)
                .Take(maxResults)
                .ToList();

            var results = similarities.Select(s => s.Embedding).ToList();

            _logger.LogInformation("Search returned {Count} results", results.Count);
            foreach (var result in similarities)
            {
                _logger.LogDebug("Found: {Id} (similarity: {Score:F4})", result.Embedding.Id, result.Similarity);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query);
            throw;
        }
    }

    public int GetEmbeddingCount() => _embeddings.Count;

    private static float CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (var i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
        {
            return 0;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }
}