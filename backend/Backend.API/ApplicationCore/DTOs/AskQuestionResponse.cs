namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// Application DTO for question answer response.
/// </summary>
public record AskQuestionResponse
{
    /// <summary>
    /// Gets the generated answer to the question.
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// Gets the source documents that were used to generate the answer.
    /// </summary>
    public required IReadOnlyList<SourceReferenceDto> Sources { get; init; }
}
