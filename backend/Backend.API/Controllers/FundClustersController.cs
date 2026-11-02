using Backend.API.ApplicationCore.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace Backend.API.Controllers;

/// <summary>
/// Serves the precomputed fund cluster map files (FundClustering pipeline output) as-is.
/// Read-only, public endpoint. Files carry Last-Modified and ETag so browsers can revalidate.
/// </summary>
[ApiController]
[Route("api/fund-clusters")]
[EnableRateLimiting("ApiRateLimit")]
public class FundClustersController : ControllerBase
{
    private const string JsonContentType = "application/json";

    private readonly IFundClusterFiles _files;
    private readonly ILogger<FundClustersController> _logger;

    public FundClustersController(IFundClusterFiles files, ILogger<FundClustersController> logger)
    {
        _files = files;
        _logger = logger;
    }

    /// <summary>
    /// Returns <c>methods.json</c>: which method files exist and which one is the default.
    /// </summary>
    [HttpGet("methods")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetMethods() =>
        JsonFile(_files.GetManifestPath()) ?? DataMissing("methods.json");

    /// <summary>
    /// Returns <c>funds.json</c>: display data per fund, shared by all methods.
    /// </summary>
    [HttpGet("funds")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetFunds() =>
        JsonFile(_files.GetFundsPath()) ?? DataMissing("funds.json");

    /// <summary>
    /// Returns the file of one clustering method listed in <c>methods.json</c>.
    /// </summary>
    /// <param name="id">Method id, e.g. <c>cosine</c>.</param>
    [HttpGet("methods/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetMethod(string id)
    {
        if (_files.GetManifestPath() is null)
            return DataMissing("methods.json");

        return JsonFile(_files.GetMethodPath(id))
               ?? NotFound(new { error = $"Unknown clustering method '{id}'." });
    }

    private IActionResult? JsonFile(string? path)
    {
        if (path is null)
            return null;

        var lastModified = System.IO.File.GetLastWriteTimeUtc(path);
        var etag = new EntityTagHeaderValue($"\"{lastModified.Ticks:x}\"");
        return PhysicalFile(path, JsonContentType, new DateTimeOffset(lastModified), etag);
    }

    private ObjectResult DataMissing(string file)
    {
        _logger.LogWarning("Fund cluster request rejected: {File} not found", file);
        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { error = "Fund cluster data is not available" });
    }
}
