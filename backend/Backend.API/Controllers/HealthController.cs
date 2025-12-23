using Backend.API.Models;
using Backend.API.Services;

using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

/// <summary>
/// Controller for health check endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IMemoryService memoryService,
        ILogger<HealthController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the health status of the backend API.
    /// </summary>
    /// <returns>Health status including embeddings information.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        var response = new HealthResponse
        {
            Status = _memoryService.IsInitialized ? "Healthy" : "Initializing",
            EmbeddingsLoaded = _memoryService.IsInitialized,
            EmbeddingCount = _memoryService.GetEmbeddingCount()
        };

        _logger.LogDebug("Health check: {Status}, Embeddings: {Count}",
            response.Status, response.EmbeddingCount);

        return Ok(response);
    }
}