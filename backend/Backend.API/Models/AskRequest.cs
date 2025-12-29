using System.ComponentModel.DataAnnotations;

namespace Backend.API.Models;

/// <summary>
/// Request model for the /api/ask endpoint.
/// </summary>
public class AskRequest
{
    /// <summary>
    /// Gets or sets the question to ask about the documents.
    /// </summary>
    [Required(ErrorMessage = "Question is required")]
    [MinLength(3, ErrorMessage = "Question must be at least 3 characters")]
    public required string Question { get; init; }
}