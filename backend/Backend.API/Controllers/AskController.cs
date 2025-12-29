using Backend.API.Models;
using Backend.API.Services;

using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

/// <summary>
/// Controller for asking questions about documents.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AskController : ControllerBase
{
    private readonly IQuestionAnsweringService _qaService;
    private readonly ILogger<AskController> _logger;

    public AskController(
        IQuestionAnsweringService qaService,
        ILogger<AskController> logger)
    {
        _qaService = qaService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question about the loaded documents.
    /// </summary>
    /// <param name="request">The question request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An answer with source references.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AskResponse>> Ask(
        [FromBody] AskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Received question: {Question}", request.Question);

            var response = await _qaService.AnswerQuestionAsync(request.Question, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question: {Question}", request.Question);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while processing your question" });
        }
    }
}