using Backend.API.Models;

namespace Backend.API.Services;

/// <summary>
/// Service for answering questions using semantic search and LLM generation.
/// </summary>
public interface IQuestionAnsweringService
{
    /// <summary>
    /// Answers a question by searching for relevant context and generating a response using an LLM.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An answer with source references.</returns>
    Task<AskResponse> AnswerQuestionAsync(string question, CancellationToken cancellationToken = default);
}
