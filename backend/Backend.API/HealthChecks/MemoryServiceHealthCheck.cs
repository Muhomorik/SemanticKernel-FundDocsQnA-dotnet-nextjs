using Backend.API.Services;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.API.HealthChecks;

/// <summary>
/// Health check for verifying that the MemoryService is initialized and has embeddings loaded.
/// Used by Azure App Service readiness probe to determine if the application is ready to serve traffic.
/// </summary>
public class MemoryServiceHealthCheck : IHealthCheck
{
    private readonly IMemoryService _memoryService;
    private readonly ILogger<MemoryServiceHealthCheck> _logger;

    public MemoryServiceHealthCheck(
        IMemoryService memoryService,
        ILogger<MemoryServiceHealthCheck> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_memoryService.IsInitialized && _memoryService.GetEmbeddingCount() > 0)
            {
                var embeddingCount = _memoryService.GetEmbeddingCount();
                _logger.LogDebug("MemoryService health check passed: {Count} embeddings loaded", embeddingCount);

                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Embeddings loaded: {embeddingCount}"));
            }

            _logger.LogWarning("MemoryService health check failed: Not initialized or no embeddings");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Embeddings not loaded or memory service not initialized"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MemoryService health check encountered an error");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "MemoryService health check failed with exception", ex));
        }
    }
}
