using System.Text.Json;
using Microsoft.Extensions.Logging;
using Preprocessor.Models;

namespace Preprocessor.Outputs;

/// <summary>
/// Outputs embeddings to a local JSON file (embeddings.json).
/// </summary>
public class JsonEmbeddingOutput : IEmbeddingOutput
{
    private readonly string _filePath;
    private readonly bool _appendMode;
    private readonly ILogger<JsonEmbeddingOutput> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public JsonEmbeddingOutput(
        string filePath,
        bool appendMode,
        ILogger<JsonEmbeddingOutput> logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _appendMode = appendMode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string DisplayName => _filePath;

    public async Task<IReadOnlyList<EmbeddingResult>> LoadExistingAsync(CancellationToken cancellationToken = default)
    {
        if (!_appendMode || !File.Exists(_filePath))
        {
            return Array.Empty<EmbeddingResult>();
        }

        _logger.LogInformation("Loading existing embeddings from {FilePath}", _filePath);

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var existing = JsonSerializer.Deserialize<List<EmbeddingResult>>(json) ?? new List<EmbeddingResult>();

            _logger.LogInformation("Loaded {Count} existing embeddings", existing.Count);
            return existing.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load existing embeddings from {FilePath}", _filePath);
            throw;
        }
    }

    public async Task SaveAsync(IReadOnlyList<EmbeddingResult> embeddings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving {Count} embeddings to {FilePath}", embeddings.Count, _filePath);

        try
        {
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Serialize and write
            var json = JsonSerializer.Serialize(embeddings, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);

            _logger.LogInformation("Successfully saved {Count} embeddings to {FilePath}", embeddings.Count, _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save embeddings to {FilePath}", _filePath);
            throw;
        }
    }
}
