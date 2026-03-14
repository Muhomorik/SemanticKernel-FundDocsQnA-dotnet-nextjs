using System.Text.Json;
using Backend.API.ApplicationCore.DTOs;
using Backend.API.ApplicationCore.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.API.Controllers;

/// <summary>
/// Controller for asking questions about documents.
/// Thin controller that validates requests and delegates to application service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ApiRateLimit")] // DoS protection - 10 requests per minute
public class AskController : ControllerBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

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
    [ProducesResponseType(typeof(AskQuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AskQuestionResponse>> Ask(
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Received question: {Question}", request.Question);

            var response = await _qaService.AnswerQuestionAsync(request, cancellationToken);

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

    /// <summary>
    /// Ask a question and receive a streaming response via Server-Sent Events.
    /// Sends sources first, then streams answer tokens as they arrive from the LLM.
    /// </summary>
    [HttpPost("stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task AskStream(
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(ModelState, cancellationToken);
            return;
        }

        _logger.LogInformation("Received streaming question: {Question}", request.Question);

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            var streamingContext = await _qaService.BeginStreamingAnswerAsync(request, cancellationToken);

            // Send sources first (available immediately from semantic search)
            await WriteSseEventAsync("sources",
                JsonSerializer.Serialize(streamingContext.Sources, s_jsonOptions), cancellationToken);

            // Stream answer tokens
            await foreach (var chunk in streamingContext.AnswerStream.WithCancellation(cancellationToken))
            {
                await WriteSseEventAsync("delta",
                    JsonSerializer.Serialize(chunk), cancellationToken);
            }

            // Signal completion
            await WriteSseEventAsync("done", "{}", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — no action needed
            _logger.LogDebug("Streaming cancelled (client disconnected)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming for question: {Question}", request.Question);
            try
            {
                await WriteSseEventAsync("error",
                    JsonSerializer.Serialize(new { message = "An error occurred while generating the answer" }, s_jsonOptions),
                    cancellationToken);
            }
            catch
            {
                // Client may have already disconnected
            }
        }
    }

    private async Task WriteSseEventAsync(string eventType, string data, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {eventType}\ndata: {data}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}