using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Preprocessor.Models;

namespace Preprocessor.Outputs;

/// <summary>
/// Outputs embeddings to backend API (Cosmos DB) via HTTP.
/// </summary>
public class CosmosDbEmbeddingOutput : IEmbeddingOutput
{
    private readonly HttpClient _httpClient;
    private readonly string _backendUrl;
    private readonly string _operation;
    private readonly int _batchSize;
    private readonly ILogger<CosmosDbEmbeddingOutput> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CosmosDbEmbeddingOutput(
        HttpClient httpClient,
        string backendUrl,
        string apiKey,
        string operation,
        int batchSize,
        ILogger<CosmosDbEmbeddingOutput> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _backendUrl = backendUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(backendUrl));
        _operation = operation?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(operation));
        _batchSize = batchSize;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or whitespace", nameof(apiKey));
        }

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_backendUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Long timeout for large uploads
    }

    public string DisplayName => $"{_backendUrl} (operation: {_operation})";

    public Task<IReadOnlyList<EmbeddingResult>> LoadExistingAsync(CancellationToken cancellationToken = default)
    {
        // Cosmos DB mode doesn't support loading existing embeddings
        // The backend manages the database state
        _logger.LogInformation("Cosmos DB mode: existing embeddings are managed by backend");
        return Task.FromResult<IReadOnlyList<EmbeddingResult>>(Array.Empty<EmbeddingResult>());
    }

    public async Task SaveAsync(IReadOnlyList<EmbeddingResult> embeddings, CancellationToken cancellationToken = default)
    {
        if (embeddings.Count == 0)
        {
            _logger.LogWarning("No embeddings to upload");
            return;
        }

        _logger.LogInformation("Uploading {Count} embeddings to backend API ({Operation} operation, batch size: {BatchSize})",
            embeddings.Count, _operation, _batchSize);

        try
        {
            switch (_operation)
            {
                case "add":
                    await AddEmbeddingsAsync(embeddings, cancellationToken);
                    break;

                case "update":
                    await UpdateEmbeddingsAsync(embeddings, cancellationToken);
                    break;

                case "replace-all":
                    await ReplaceAllEmbeddingsAsync(embeddings, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported operation: {_operation}");
            }

            _logger.LogInformation("Successfully uploaded {Count} embeddings to backend API", embeddings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload embeddings to backend API");
            throw;
        }
    }

    private async Task AddEmbeddingsAsync(IReadOnlyList<EmbeddingResult> embeddings, CancellationToken cancellationToken)
    {
        // Upload in batches
        var batches = embeddings
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / _batchSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        _logger.LogInformation("Uploading {TotalCount} embeddings in {BatchCount} batches",
            embeddings.Count, batches.Count);

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            _logger.LogInformation("Uploading batch {Current}/{Total} ({Count} embeddings)",
                i + 1, batches.Count, batch.Count);

            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", batch, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Batch {Current}/{Total} uploaded successfully", i + 1, batches.Count);
        }
    }

    private async Task UpdateEmbeddingsAsync(IReadOnlyList<EmbeddingResult> embeddings, CancellationToken cancellationToken)
    {
        // Group by source file
        var groupedBySource = embeddings
            .GroupBy(e => e.Source)
            .ToList();

        _logger.LogInformation("Updating embeddings for {FileCount} source files", groupedBySource.Count);

        foreach (var group in groupedBySource)
        {
            var sourceFile = group.Key;
            var embeddingsForFile = group.ToList();

            _logger.LogInformation("Updating {Count} embeddings for source file: {SourceFile}",
                embeddingsForFile.Count, sourceFile);

            var encodedSourceFile = Uri.EscapeDataString(sourceFile);
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/embeddings/{encodedSourceFile}",
                embeddingsForFile,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Updated embeddings for source file: {SourceFile}", sourceFile);
        }
    }

    private async Task ReplaceAllEmbeddingsAsync(IReadOnlyList<EmbeddingResult> embeddings, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Replacing all embeddings in backend (this will delete existing data)");

        // Upload in batches
        var batches = embeddings
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / _batchSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            _logger.LogInformation("Uploading batch {Current}/{Total} ({Count} embeddings) for replace-all operation",
                i + 1, batches.Count, batch.Count);

            var response = await _httpClient.PostAsJsonAsync("/api/embeddings/replace-all", batch, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Batch {Current}/{Total} uploaded successfully", i + 1, batches.Count);
        }
    }
}
