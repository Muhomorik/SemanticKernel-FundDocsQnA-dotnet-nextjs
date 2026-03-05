using Backend.API.Infrastructure.FundData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.API.HealthChecks;

/// <summary>
/// Health check for Azure SQL Database connectivity.
/// Only registered when AzureSqlConnectionString is configured.
/// </summary>
public class AzureSqlHealthCheck : IHealthCheck
{
    private readonly FundDataDbContext _context;
    private readonly ILogger<AzureSqlHealthCheck> _logger;

    public AzureSqlHealthCheck(FundDataDbContext context, ILogger<AzureSqlHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _context.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogWarning("Azure SQL health check: Cannot connect to database");
                return HealthCheckResult.Unhealthy("Cannot connect to Azure SQL Database");
            }

            var profileCount = await _context.FundProfiles.CountAsync(cancellationToken);

            _logger.LogDebug("Azure SQL health check passed: {Count} fund profiles", profileCount);
            return HealthCheckResult.Healthy($"Azure SQL connected: {profileCount} fund profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure SQL health check failed");
            return HealthCheckResult.Unhealthy("Azure SQL health check failed", ex);
        }
    }
}
