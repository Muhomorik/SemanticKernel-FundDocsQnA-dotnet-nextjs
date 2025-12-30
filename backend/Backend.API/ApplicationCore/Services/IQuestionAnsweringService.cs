using Backend.API.ApplicationCore.DTOs;

namespace Backend.API.ApplicationCore.Services;

/// <summary>
/// Application service for question answering use case.
/// Orchestrates domain services and infrastructure.
/// </summary>
public interface IQuestionAnsweringService
{
    Task<AskQuestionResponse> AnswerQuestionAsync(
        AskQuestionRequest request,
        CancellationToken cancellationToken = default);
}
