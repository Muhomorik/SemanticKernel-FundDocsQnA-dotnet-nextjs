using Backend.API.ApplicationCore.DTOs;
using Backend.API.Configuration;
using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.API.Controllers;

/// <summary>
/// Controller for managing document embeddings in Cosmos DB.
/// Only available when VectorStorageType is CosmosDb.
/// Protected by API key authentication (ApiKeyAuthenticationMiddleware).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiRateLimit")]
public class EmbeddingsController : ControllerBase
{
    private readonly IDocumentRepository _repository;
    private readonly BackendOptions _options;
    private readonly ILogger<EmbeddingsController> _logger;

    public EmbeddingsController(
        IDocumentRepository repository,
        BackendOptions options,
        ILogger<EmbeddingsController> logger)
    {
        _repository = repository;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Adds new embeddings to the vector store.
    /// </summary>
    /// <param name="request">Request containing embeddings to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EmbeddingOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmbeddingOperationResponse>> AddEmbeddings(
        [FromBody] AddEmbeddingsRequest request,
        CancellationToken cancellationToken)
    {
        // Check storage type
        if (_options.VectorStorageType != VectorStorageType.CosmosDb)
        {
            _logger.LogWarning("Add embeddings request rejected: VectorStorageType is {Type}, not CosmosDb",
                _options.VectorStorageType);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Embedding management only available with Cosmos DB storage" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Adding {Count} embeddings", request.Embeddings.Count);

            // Convert DTOs to domain models
            var chunks = request.Embeddings.Select(dto => DocumentChunk.Create(
                dto.Id,
                dto.Text,
                dto.Embedding,
                dto.SourceFile,
                dto.Page)).ToList();

            await _repository.AddChunksAsync(chunks, cancellationToken);

            _logger.LogInformation("Successfully added {Count} embeddings", chunks.Count);

            return Ok(new EmbeddingOperationResponse
            {
                Success = true,
                Message = $"Successfully added {chunks.Count} embeddings",
                Count = chunks.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding embeddings");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while adding embeddings" });
        }
    }

    /// <summary>
    /// Updates embeddings for a specific source file.
    /// </summary>
    /// <param name="sourceFile">Source filename to update.</param>
    /// <param name="request">Request containing new embeddings for the source file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result.</returns>
    [HttpPut("{sourceFile}")]
    [ProducesResponseType(typeof(EmbeddingOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmbeddingOperationResponse>> UpdateEmbeddings(
        string sourceFile,
        [FromBody] AddEmbeddingsRequest request,
        CancellationToken cancellationToken)
    {
        // Check storage type
        if (_options.VectorStorageType != VectorStorageType.CosmosDb)
        {
            _logger.LogWarning("Update embeddings request rejected: VectorStorageType is {Type}, not CosmosDb",
                _options.VectorStorageType);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Embedding management only available with Cosmos DB storage" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Updating embeddings for source: {Source} ({Count} embeddings)",
                sourceFile, request.Embeddings.Count);

            // Validate that all embeddings belong to the specified source file
            var mismatchedSources = request.Embeddings
                .Where(e => e.SourceFile != sourceFile)
                .Select(e => e.SourceFile)
                .Distinct()
                .ToList();

            if (mismatchedSources.Any())
            {
                _logger.LogWarning("Source file mismatch: expected {Expected}, found {Found}",
                    sourceFile, string.Join(", ", mismatchedSources));
                return BadRequest(new
                {
                    error = $"All embeddings must have sourceFile = '{sourceFile}'. " +
                            $"Found mismatched sources: {string.Join(", ", mismatchedSources)}"
                });
            }

            // Delete existing embeddings for this source
            await _repository.DeleteChunksBySourceAsync(sourceFile, cancellationToken);

            // Add new embeddings
            var chunks = request.Embeddings.Select(dto => DocumentChunk.Create(
                dto.Id,
                dto.Text,
                dto.Embedding,
                dto.SourceFile,
                dto.Page)).ToList();

            await _repository.AddChunksAsync(chunks, cancellationToken);

            _logger.LogInformation("Successfully updated embeddings for source: {Source}", sourceFile);

            return Ok(new EmbeddingOperationResponse
            {
                Success = true,
                Message = $"Successfully updated embeddings for {sourceFile}",
                Count = chunks.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating embeddings for source: {Source}", sourceFile);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating embeddings" });
        }
    }

    /// <summary>
    /// Deletes all embeddings for a specific source file.
    /// </summary>
    /// <param name="sourceFile">Source filename to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result.</returns>
    [HttpDelete("{sourceFile}")]
    [ProducesResponseType(typeof(EmbeddingOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmbeddingOperationResponse>> DeleteEmbeddings(
        string sourceFile,
        CancellationToken cancellationToken)
    {
        // Check storage type
        if (_options.VectorStorageType != VectorStorageType.CosmosDb)
        {
            _logger.LogWarning("Delete embeddings request rejected: VectorStorageType is {Type}, not CosmosDb",
                _options.VectorStorageType);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Embedding management only available with Cosmos DB storage" });
        }

        try
        {
            _logger.LogInformation("Deleting embeddings for source: {Source}", sourceFile);

            await _repository.DeleteChunksBySourceAsync(sourceFile, cancellationToken);

            _logger.LogInformation("Successfully deleted embeddings for source: {Source}", sourceFile);

            return Ok(new EmbeddingOperationResponse
            {
                Success = true,
                Message = $"Successfully deleted embeddings for {sourceFile}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting embeddings for source: {Source}", sourceFile);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting embeddings" });
        }
    }

    /// <summary>
    /// Replaces ALL embeddings in the vector store with a new set.
    /// WARNING: This is a destructive operation that deletes all existing data.
    /// </summary>
    /// <param name="request">Request containing new embeddings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result.</returns>
    [HttpPost("replace-all")]
    [ProducesResponseType(typeof(EmbeddingOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmbeddingOperationResponse>> ReplaceAllEmbeddings(
        [FromBody] ReplaceAllEmbeddingsRequest request,
        CancellationToken cancellationToken)
    {
        // Check storage type
        if (_options.VectorStorageType != VectorStorageType.CosmosDb)
        {
            _logger.LogWarning("Replace all embeddings request rejected: VectorStorageType is {Type}, not CosmosDb",
                _options.VectorStorageType);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Embedding management only available with Cosmos DB storage" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogWarning("Replacing ALL embeddings with {Count} new embeddings (destructive operation)",
                request.Embeddings.Count);

            // Convert DTOs to domain models
            var chunks = request.Embeddings.Select(dto => DocumentChunk.Create(
                dto.Id,
                dto.Text,
                dto.Embedding,
                dto.SourceFile,
                dto.Page)).ToList();

            await _repository.ReplaceAllChunksAsync(chunks, cancellationToken);

            _logger.LogInformation("Successfully replaced all embeddings: {Count} chunks now in database", chunks.Count);

            return Ok(new EmbeddingOperationResponse
            {
                Success = true,
                Message = $"Successfully replaced all embeddings with {chunks.Count} new chunks",
                Count = chunks.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing all embeddings");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while replacing embeddings" });
        }
    }
}
