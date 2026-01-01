using Backend.API.Domain.Interfaces;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.API.HealthChecks;

/// <summary>
/// Health check for verifying that the DocumentRepository is initialized and has chunks loaded.
/// Used by Azure App Service readiness probe to determine if the application is ready to serve traffic.
/// </summary>
public class MemoryServiceHealthCheck : IHealthCheck
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<MemoryServiceHealthCheck> _logger;

    public MemoryServiceHealthCheck(
        IDocumentRepository repository,
        ILogger<MemoryServiceHealthCheck> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_repository.IsInitialized && _repository.GetChunkCount() > 0)
            {
                var chunkCount = _repository.GetChunkCount();
                _logger.LogDebug("Document repository health check passed: {Count} chunks loaded", chunkCount);

                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Document chunks loaded: {chunkCount}"));
            }

            _logger.LogWarning("Document repository health check failed: Not initialized or no chunks");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Document chunks not loaded or repository not initialized"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document repository health check encountered an error");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Document repository health check failed with exception", ex));
        }
    }
}
