using System.ComponentModel.DataAnnotations;

using Backend.API.ApplicationCore.DTOs.OwnershipFlow;
using Backend.API.ApplicationCore.Services;
using Backend.API.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.API.Controllers;

/// <summary>
/// Controller for ownership flow (Sankey diagram) data.
/// Read-only, public endpoint. Only available when AzureSqlConnectionString is configured.
/// </summary>
[ApiController]
[Route("api/ownership-flow")]
[EnableRateLimiting("ApiRateLimit")]
public class OwnershipFlowController : ControllerBase
{
    private readonly IOwnershipFlowService _ownershipFlowService;
    private readonly BackendOptions _options;
    private readonly ILogger<OwnershipFlowController> _logger;

    public OwnershipFlowController(
        IOwnershipFlowService ownershipFlowService,
        BackendOptions options,
        ILogger<OwnershipFlowController> logger)
    {
        _ownershipFlowService = ownershipFlowService;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Returns available weekly and monthly time periods for the ownership flow selector.
    /// </summary>
    [HttpGet("periods")]
    [ProducesResponseType(typeof(OwnershipFlowPeriodsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<OwnershipFlowPeriodsResponse> GetPeriods()
    {
        if (!IsAzureSqlConfigured())
            return AzureSqlNotConfiguredResponse();

        var result = _ownershipFlowService.GetAvailablePeriods();
        return Ok(result);
    }

    /// <summary>
    /// Returns ownership flow data for both category-level and fund-level Sankey diagrams.
    /// </summary>
    /// <param name="from">Start date of the period (inclusive, ISO 8601: yyyy-MM-dd).</param>
    /// <param name="to">End date of the period (inclusive, ISO 8601: yyyy-MM-dd).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(OwnershipFlowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OwnershipFlowResponse>> GetOwnershipFlow(
        [FromQuery, Required] DateOnly from,
        [FromQuery, Required] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (from > to)
            return BadRequest(new { error = "Parameter 'from' must not be later than 'to'." });

        if (to.DayNumber - from.DayNumber > 365)
            return BadRequest(new { error = "Date range cannot exceed 365 days." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (from > today)
            return BadRequest(new { error = "Parameter 'from' cannot be in the future." });

        if (!IsAzureSqlConfigured())
            return AzureSqlNotConfiguredResponse();

        try
        {
            var result = await _ownershipFlowService.GetOwnershipFlowAsync(from, to, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing ownership flow for {From} to {To}", from, to);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while computing ownership flow data" });
        }
    }

    private bool IsAzureSqlConfigured() =>
        !string.IsNullOrWhiteSpace(_options.AzureSqlConnectionString);

    private ObjectResult AzureSqlNotConfiguredResponse()
    {
        _logger.LogWarning("Ownership flow request rejected: Azure SQL not configured");
        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { error = "Ownership flow requires Azure SQL Database configuration" });
    }
}
