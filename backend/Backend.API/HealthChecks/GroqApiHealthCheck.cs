using System.Net;

using Backend.API.Configuration;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.API.HealthChecks;

/// <summary>
/// Health check for verifying that the Groq API is reachable.
/// Used by Azure App Service readiness probe to ensure external dependencies are available.
/// </summary>
public class GroqApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BackendOptions _options;
    private readonly ILogger<GroqApiHealthCheck> _logger;

    public GroqApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        BackendOptions options,
        ILogger<GroqApiHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Simple HEAD request to check if Groq API is reachable
            var request = new HttpRequestMessage(HttpMethod.Head, _options.GroqApiUrl);
            var response = await httpClient.SendAsync(request, cancellationToken);

            // 401 Unauthorized is fine - it means the API is reachable but we didn't provide credentials
            // We're only checking connectivity, not authentication
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogDebug("Groq API health check passed: {StatusCode}", response.StatusCode);
                return HealthCheckResult.Healthy("Groq API is reachable");
            }

            _logger.LogWarning("Groq API health check degraded: {StatusCode}", response.StatusCode);
            return HealthCheckResult.Degraded($"Groq API returned unexpected status: {response.StatusCode}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Groq API health check timed out");
            return HealthCheckResult.Degraded("Groq API health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API health check failed");
            return HealthCheckResult.Unhealthy("Groq API is unreachable", ex);
        }
    }
}