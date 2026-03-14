using Backend.API.ApplicationCore.DTOs;
using Backend.API.ApplicationCore.Services;
using Backend.API.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.API.Controllers;

/// <summary>
/// Controller for syncing fund data from YieldRaccoon to Azure SQL.
/// Only available when AzureSqlConnectionString is configured.
/// Protected by API key authentication (ApiKeyAuthenticationMiddleware).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiRateLimit")]
public class FundsController : ControllerBase
{
    private readonly IFundSyncService _fundSyncService;
    private readonly BackendOptions _options;
    private readonly ILogger<FundsController> _logger;

    public FundsController(
        IFundSyncService fundSyncService,
        BackendOptions options,
        ILogger<FundsController> logger)
    {
        _fundSyncService = fundSyncService;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Syncs a batch of fund profiles + daily snapshots from a crawl session (fund list page).
    /// </summary>
    [HttpPost("list")]
    [ProducesResponseType(typeof(FundSyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FundSyncResponse>> SyncFromFundList(
        [FromBody] FundListSyncRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAzureSqlConfigured())
        {
            return AzureSqlNotConfiguredResponse();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Fund list sync: {Count} funds", request.Funds.Count);
            var result = await _fundSyncService.SyncFromFundListAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing fund list data");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while syncing fund list data" });
        }
    }

    /// <summary>
    /// Syncs a single fund profile + chart history records from a fund detail page.
    /// </summary>
    [HttpPost("about")]
    [ProducesResponseType(typeof(FundSyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FundSyncResponse>> SyncFromFundAbout(
        [FromBody] FundAboutSyncRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAzureSqlConfigured())
        {
            return AzureSqlNotConfiguredResponse();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Fund about sync for {Isin}", request.Profile.Isin);
            var result = await _fundSyncService.SyncFromFundAboutAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing fund about data");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while syncing fund about data" });
        }
    }

    /// <summary>
    /// Full-sync path used by CloudSyncWindow.
    /// Guarantees the fund profile FK exists (insert-if-not-exists); upserts history records
    /// with sparse semantics (non-null fields only; Nav/NavDate never overwritten).
    /// </summary>
    [HttpPost("full-sync")]
    [ProducesResponseType(typeof(FundSyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FundSyncResponse>> SyncFullHistory(
        [FromBody] FundFullHistorySyncRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAzureSqlConfigured())
        {
            return AzureSqlNotConfiguredResponse();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Full history sync for {Isin}: {Count} records",
                request.Profile.Isin, request.HistoryRecords.Count);
            var result = await _fundSyncService.SyncFullHistoryAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full history sync");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred during full history sync" });
        }
    }

    private bool IsAzureSqlConfigured() =>
        !string.IsNullOrWhiteSpace(_options.AzureSqlConnectionString);

    private ObjectResult AzureSqlNotConfiguredResponse()
    {
        _logger.LogWarning("Fund sync request rejected: Azure SQL not configured");
        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { error = "Fund data sync requires Azure SQL Database configuration" });
    }
}
