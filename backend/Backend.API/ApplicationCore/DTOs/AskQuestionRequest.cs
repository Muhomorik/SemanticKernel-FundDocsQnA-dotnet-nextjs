using System.ComponentModel.DataAnnotations;

namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// Application DTO for question asking request.
/// Decoupled from presentation layer concerns.
/// </summary>
public record AskQuestionRequest
{
    /// <summary>
    /// Gets the question to ask about the documents.
    /// </summary>
    [Required(ErrorMessage = "Question is required")]
    [MinLength(3, ErrorMessage = "Question must be at least 3 characters")]
    public required string Question { get; init; }
}
