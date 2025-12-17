using System.Text.Json;
using Microsoft.Extensions.Logging;
using Preprocessor.Extractors;
using Preprocessor.Models;

namespace Preprocessor.Services;

/// <summary>
/// Main orchestration service for the preprocessor.
/// Coordinates PDF extraction and embedding generation.
/// </summary>
public class PreprocessorService
{
    private readonly IEnumerable<IPdfExtractor> _extractors;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<PreprocessorService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public PreprocessorService(
        IEnumerable<IPdfExtractor> extractors,
        IEmbeddingService embeddingService,
        ILogger<PreprocessorService> logger)
    {
        _extractors = extractors;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// Processes PDF files according to the provided options.
    /// </summary>
    /// <param name="options">Processing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    public async Task<int> ProcessAsync(Options options, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate options
            var errors = options.Validate().ToList();
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    _logger.LogError("Validation error: {Error}", error);
                }
                return 1;
            }

            // Find the appropriate extractor
            var extractor = _extractors.FirstOrDefault(e =>
                e.MethodName.Equals(options.Method, StringComparison.OrdinalIgnoreCase));

            if (extractor == null)
            {
                _logger.LogError("No extractor found for method: {Method}", options.Method);
                return 1;
            }

            _logger.LogInformation("Using extraction method: {Method}", extractor.MethodName);

            // Find all PDF files
            var pdfFiles = Directory.GetFiles(options.Input, "*.pdf", SearchOption.TopDirectoryOnly);

            if (pdfFiles.Length == 0)
            {
                _logger.LogWarning("No PDF files found in {InputDir}", options.Input);
                return 0;
            }

            _logger.LogInformation("Found {Count} PDF files to process", pdfFiles.Length);

            // Load existing results if appending
            var existingResults = new List<EmbeddingResult>();
            if (options.Append && File.Exists(options.Output))
            {
                _logger.LogInformation("Loading existing embeddings from {Output}", options.Output);
                var existingJson = await File.ReadAllTextAsync(options.Output, cancellationToken);
                existingResults = JsonSerializer.Deserialize<List<EmbeddingResult>>(existingJson) ?? new List<EmbeddingResult>();
                _logger.LogInformation("Loaded {Count} existing embeddings", existingResults.Count);
            }

            var allResults = new List<EmbeddingResult>(existingResults);

            // Process each PDF
            foreach (var pdfFile in pdfFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Processing: {FileName}", Path.GetFileName(pdfFile));

                try
                {
                    // Extract chunks
                    var chunks = await extractor.ExtractAsync(pdfFile, cancellationToken);
                    var chunkList = chunks.ToList();

                    if (chunkList.Count == 0)
                    {
                        _logger.LogWarning("No text extracted from {FileName}", Path.GetFileName(pdfFile));
                        continue;
                    }

                    _logger.LogInformation("Extracted {Count} chunks from {FileName}", chunkList.Count, Path.GetFileName(pdfFile));

                    // Generate embeddings for each chunk
                    foreach (var chunk in chunkList)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content, cancellationToken);

                        var result = new EmbeddingResult
                        {
                            Id = GenerateId(chunk),
                            Text = chunk.Content,
                            Embedding = embedding,
                            Source = chunk.SourceFile,
                            Page = chunk.PageNumber
                        };

                        allResults.Add(result);
                    }

                    _logger.LogInformation("Generated embeddings for {FileName}", Path.GetFileName(pdfFile));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing {FileName}", Path.GetFileName(pdfFile));
                }
            }

            // Save results
            _logger.LogInformation("Saving {Count} embeddings to {Output}", allResults.Count, options.Output);

            var outputDir = Path.GetDirectoryName(options.Output);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var json = JsonSerializer.Serialize(allResults, JsonOptions);
            await File.WriteAllTextAsync(options.Output, json, cancellationToken);

            _logger.LogInformation("Successfully processed {Count} PDF files", pdfFiles.Length);
            return 0;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processing was cancelled");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during processing");
            return 1;
        }
    }

    private static string GenerateId(DocumentChunk chunk)
    {
        var fileName = Path.GetFileNameWithoutExtension(chunk.SourceFile)
            .ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("-", "_");

        return $"{fileName}_page{chunk.PageNumber}_chunk{chunk.ChunkIndex}";
    }
}
