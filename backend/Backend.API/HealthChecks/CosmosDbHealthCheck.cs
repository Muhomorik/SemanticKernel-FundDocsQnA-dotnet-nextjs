using Backend.API.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.API.HealthChecks;

/// <summary>
/// Health check for Cosmos DB connectivity and vector store accessibility.
/// Only registered when VectorStorageType is CosmosDb.
/// Verifies database, container existence, and basic query functionality.
/// </summary>
public class CosmosDbHealthCheck : IHealthCheck
{
    private readonly CosmosClient _cosmosClient;
    private readonly BackendOptions _options;
    private readonly ILogger<CosmosDbHealthCheck> _logger;

    public CosmosDbHealthCheck(
        CosmosClient cosmosClient,
        BackendOptions options,
        ILogger<CosmosDbHealthCheck> logger)
    {
        _cosmosClient = cosmosClient;
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get database reference
            var database = _cosmosClient.GetDatabase(_options.CosmosDbDatabaseName);

            // Get container reference
            var container = database.GetContainer(_options.CosmosDbContainerName);

            // Perform simple count query to verify accessibility
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            var iterator = container.GetItemQueryIterator<int>(query);

            if (!iterator.HasMoreResults)
            {
                _logger.LogWarning("Cosmos DB health check: No results from count query");
                return HealthCheckResult.Unhealthy(
                    $"Cosmos DB container '{_options.CosmosDbContainerName}' query returned no results");
            }

            var response = await iterator.ReadNextAsync(cancellationToken);
            var count = response.FirstOrDefault();

            _logger.LogDebug("Cosmos DB health check passed: {Count} documents in container", count);

            return HealthCheckResult.Healthy(
                $"Cosmos DB connected: {count} documents in '{_options.CosmosDbDatabaseName}/{_options.CosmosDbContainerName}'");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError(ex, "Cosmos DB health check failed: Database or container not found");
            return HealthCheckResult.Unhealthy(
                $"Cosmos DB database '{_options.CosmosDbDatabaseName}' or container '{_options.CosmosDbContainerName}' not found",
                ex);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError(ex, "Cosmos DB health check failed: Authentication error");
            return HealthCheckResult.Unhealthy(
                "Cosmos DB authentication failed. Check Managed Identity configuration or connection string.",
                ex);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB health check failed: {StatusCode}", ex.StatusCode);
            return HealthCheckResult.Unhealthy(
                $"Cosmos DB error: {ex.StatusCode} - {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cosmos DB health check encountered unexpected error");
            return HealthCheckResult.Unhealthy(
                "Cosmos DB health check failed with exception",
                ex);
        }
    }
}
