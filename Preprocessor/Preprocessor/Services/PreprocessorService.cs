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
    private readonly IChunkSanitizer _chunkSanitizer;
    private readonly ILogger<PreprocessorService> _logger;

    public PreprocessorService(
        IEnumerable<IPdfExtractor> extractors,
        IEmbeddingService embeddingService,
        IChunkSanitizer chunkSanitizer,
        ILogger<PreprocessorService> logger)
    {
        _extractors = extractors;
        _embeddingService = embeddingService;
        _chunkSanitizer = chunkSanitizer;
        _logger = logger;
    }

    /// <summary>
    /// Processes PDF files according to the provided options.
    /// </summary>
    /// <param name="options">Processing configuration (extraction method, input directory).</param>
    /// <param name="output">Output handler for embeddings (behavior).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    public async Task<int> ProcessAsync(
        ProcessingOptions options,
        Outputs.IEmbeddingOutput output,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the appropriate extractor
            var extractor = _extractors.FirstOrDefault(e =>
                e.MethodName.Equals(options.Method, StringComparison.OrdinalIgnoreCase));

            if (extractor == null)
            {
                _logger.LogError("No extractor found for method: {Method}", options.Method);
                return 1;
            }

            _logger.LogInformation("Using extraction method: {Method}", extractor.MethodName);
            _logger.LogInformation("Output destination: {Destination}", output.DisplayName);

            // Convert relative paths to absolute paths based on current working directory
            var inputPath = Path.GetFullPath(options.InputDirectory);

            // Find all PDF files
            var pdfFiles = Directory.GetFiles(inputPath, "*.pdf", SearchOption.TopDirectoryOnly);

            if (pdfFiles.Length == 0)
            {
                _logger.LogWarning("No PDF files found in {InputDir}", inputPath);
                return 0;
            }

            _logger.LogInformation("Found {Count} PDF files to process", pdfFiles.Length);

            // Load existing embeddings from output handler
            var existingResults = await output.LoadExistingAsync(cancellationToken);
            var allResults = new List<EmbeddingResult>(existingResults);

            _logger.LogInformation("Starting with {Count} existing embeddings", existingResults.Count);

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

                    _logger.LogInformation("Extracted {Count} chunks from {FileName}", chunkList.Count,
                        Path.GetFileName(pdfFile));

                    // Sanitize chunks to remove noise patterns
                    chunkList = [.._chunkSanitizer.Sanitize(chunkList)];

                    // Generate embeddings for each chunk
                    foreach (var chunk in chunkList)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var embedding =
                            await _embeddingService.GenerateEmbeddingAsync(chunk.Content, cancellationToken);

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

            // Save results via output handler
            await output.SaveAsync(allResults.AsReadOnly(), cancellationToken);

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