using System.ComponentModel.DataAnnotations;
using Backend.API.ApplicationCore.Validation;

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
    [MaxLength(500, ErrorMessage = "Question must not exceed 500 characters")]
    [SafeQuestion]
    public required string Question { get; init; }
}
