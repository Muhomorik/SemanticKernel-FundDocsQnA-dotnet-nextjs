using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using Preprocessor.Models;

namespace Preprocessor.Outputs;

/// <summary>
/// Request wrapper matching Backend.API's AddEmbeddingsRequest structure.
/// Backend API uses default camelCase JSON serialization.
/// </summary>
internal record BackendAddEmbeddingsRequest
{
    [JsonPropertyName("embeddings")] public required List<BackendEmbeddingDto> Embeddings { get; init; }
}

/// <summary>
/// Request wrapper matching Backend.API's ReplaceAllEmbeddingsRequest structure.
/// Backend API uses default camelCase JSON serialization.
/// </summary>
internal record BackendReplaceAllEmbeddingsRequest
{
    [JsonPropertyName("embeddings")] public required List<BackendEmbeddingDto> Embeddings { get; init; }
}

/// <summary>
/// Outputs embeddings to backend API (Cosmos DB) via HTTP.
///
/// Includes rate limiting to prevent exceeding Cosmos DB free tier throughput (1000 RU/s, 400 RU/s provisioned).
/// Default 8000ms delay between batches ensures ~290 RU/s average across 950 embeddings in 10 batches (~130 seconds total).
/// This leaves comfortable headroom under the 400 RU/s limit.
///
/// For reference with 68 PDFs × 14 chunks = 950 embeddings, batch size 100:
/// - 9 intervals × 8000ms = 72 seconds of delays
/// - ~50 seconds upload time
/// - Total ~122-130 seconds
/// - RU/s = 38,000 RU ÷ 130s = ~290 RU/s (safe margin)
/// </summary>
public class CosmosDbEmbeddingOutput : IEmbeddingOutput
{
    private readonly HttpClient _httpClient;
    private readonly string _backendUrl;
    private readonly string _operation;
    private readonly int _batchSize;
    /// <summary>Milliseconds to wait between batch uploads to prevent Cosmos DB throttling (default: 8000ms)</summary>
    private readonly int _delayBetweenBatchesMs;
    /// <summary>Maximum retry attempts for 429 throttling responses with exponential backoff (default: 3)</summary>
    private readonly int _maxRetries;
    private readonly ILogger<CosmosDbEmbeddingOutput> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null // Use property names as-is (PascalCase from JsonPropertyName attributes)
    };

    /// <summary>
    /// Creates a new Cosmos DB embedding output instance with configurable rate limiting.
    /// </summary>
    /// <param name="delayBetweenBatchesMs">
    /// Milliseconds to wait between batch uploads. Default 8000ms ensures ~290 RU/s average,
    /// staying well below the 400 RU/s Cosmos DB provisioned tier limit.
    /// </param>
    /// <param name="maxRetries">
    /// Maximum retry attempts for 429 (TooManyRequests) responses.
    /// Uses exponential backoff: 1s, 2s, 4s between retries.
    /// </param>
    public CosmosDbEmbeddingOutput(
        HttpClient httpClient,
        string backendUrl,
        string apiKey,
        string operation,
        int batchSize,
        ILogger<CosmosDbEmbeddingOutput> logger,
        int delayBetweenBatchesMs = 8000,
        int maxRetries = 3)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _backendUrl = backendUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(backendUrl));
        _operation = operation?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(operation));
        _batchSize = batchSize;
        _delayBetweenBatchesMs = delayBetweenBatchesMs;
        _maxRetries = maxRetries;
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

    public async Task SaveAsync(IReadOnlyList<EmbeddingResult> embeddings,
        CancellationToken cancellationToken = default)
    {
        if (embeddings.Count == 0)
        {
            _logger.LogWarning("No embeddings to upload");
            return;
        }

        _logger.LogInformation(
            "Uploading {Count} embeddings to backend API ({Operation} operation, batch size: {BatchSize})",
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

    private async Task AddEmbeddingsAsync(IReadOnlyList<EmbeddingResult> embeddings,
        CancellationToken cancellationToken)
    {
        // Upload in batches
        var batches = embeddings
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / _batchSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        _logger.LogInformation("Uploading {TotalCount} embeddings in {BatchCount} batches (delay: {DelayMs}ms between batches)",
            embeddings.Count, batches.Count, _delayBetweenBatchesMs);

        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            _logger.LogInformation("Uploading batch {Current}/{Total} ({Count} embeddings)",
                i + 1, batches.Count, batch.Count);

            // Convert to backend DTOs and wrap in request object
            var backendDtos = batch.Select(e => new BackendEmbeddingDto
            {
                Id = e.Id,
                Text = e.Text,
                Embedding = e.Embedding,
                SourceFile = e.Source, // Map Source -> SourceFile
                Page = e.Page
            }).ToList();

            var request = new BackendAddEmbeddingsRequest { Embeddings = backendDtos };

            await SendWithRetryAsync(
                () => _httpClient.PostAsJsonAsync("/api/embeddings", request, JsonOptions, cancellationToken),
                $"batch {i + 1}/{batches.Count}",
                cancellationToken);

            _logger.LogInformation("Batch {Current}/{Total} uploaded successfully", i + 1, batches.Count);

            // Delay between batches (except after last batch)
            if (i < batches.Count - 1)
            {
                _logger.LogInformation("Waiting {DelayMs}ms before next batch", _delayBetweenBatchesMs);
                await Task.Delay(_delayBetweenBatchesMs, cancellationToken);
            }
        }
    }

    private async Task UpdateEmbeddingsAsync(IReadOnlyList<EmbeddingResult> embeddings,
        CancellationToken cancellationToken)
    {
        // Group by source file
        var groupedBySource = embeddings
            .GroupBy(e => e.Source)
            .ToList();

        _logger.LogInformation("Updating embeddings for {FileCount} source files (delay: {DelayMs}ms between files)",
            groupedBySource.Count, _delayBetweenBatchesMs);

        var fileIndex = 0;
        foreach (var group in groupedBySource)
        {
            fileIndex++;
            var sourceFile = group.Key;
            var embeddingsForFile = group.ToList();

            _logger.LogInformation("Updating {Count} embeddings for source file {Current}/{Total}: {SourceFile}",
                embeddingsForFile.Count, fileIndex, groupedBySource.Count, sourceFile);

            var encodedSourceFile = Uri.EscapeDataString(sourceFile);
            await SendWithRetryAsync(
                () => _httpClient.PutAsJsonAsync(
                    $"/api/embeddings/{encodedSourceFile}",
                    embeddingsForFile,
                    JsonOptions,
                    cancellationToken),
                $"file {fileIndex}/{groupedBySource.Count}",
                cancellationToken);

            _logger.LogInformation("Updated embeddings for source file: {SourceFile}", sourceFile);

            // Delay between file updates (except after last file)
            if (fileIndex < groupedBySource.Count)
            {
                _logger.LogInformation("Waiting {DelayMs}ms before next file update", _delayBetweenBatchesMs);
                await Task.Delay(_delayBetweenBatchesMs, cancellationToken);
            }
        }
    }

    private async Task ReplaceAllEmbeddingsAsync(IReadOnlyList<EmbeddingResult> embeddings,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Replacing all embeddings in backend (this will delete existing data)");

        // Convert to backend DTOs and wrap in request object
        var backendDtos = embeddings.Select(e => new BackendEmbeddingDto
        {
            Id = e.Id,
            Text = e.Text,
            Embedding = e.Embedding,
            SourceFile = e.Source, // Map Source -> SourceFile
            Page = e.Page
        }).ToList();

        var request = new BackendReplaceAllEmbeddingsRequest { Embeddings = backendDtos };

        _logger.LogInformation("Uploading {Count} embeddings for replace-all operation", embeddings.Count);

        await SendWithRetryAsync(
            () => _httpClient.PostAsJsonAsync("/api/embeddings/replace-all", request, JsonOptions, cancellationToken),
            "replace-all",
            cancellationToken);

        _logger.LogInformation("Successfully replaced all embeddings");
    }

    /// <summary>
    /// Sends a request with exponential backoff retry logic for Cosmos DB throttling (429 responses).
    /// Implements the standard exponential backoff pattern: 1s, 2s, 4s between retries.
    /// </summary>
    private async Task SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> sendAsync,
        string operationName,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;

        while (true)
        {
            try
            {
                var response = await sendAsync();

                // Handle 429 (TooManyRequests) with exponential backoff
                // Cosmos DB returns 429 when RU/s throughput limit is exceeded
                if ((int)response.StatusCode == 429)
                {
                    if (retryCount >= _maxRetries)
                    {
                        _logger.LogError("Operation {OperationName} failed: Cosmos DB throttling after {RetryCount} retries",
                            operationName, retryCount);
                        response.EnsureSuccessStatusCode(); // Throw HttpRequestException
                    }

                    // Exponential backoff: 1s, 2s, 4s
                    var delayMs = (int)Math.Pow(2, retryCount) * 1000;
                    _logger.LogWarning("Operation {OperationName} throttled (429). Retrying in {DelayMs}ms (attempt {Current}/{Max})",
                        operationName, delayMs, retryCount + 1, _maxRetries);

                    await Task.Delay(delayMs, cancellationToken);
                    retryCount++;
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation {OperationName} failed: {Message}", operationName, ex.Message);
                throw;
            }
        }
    }
}