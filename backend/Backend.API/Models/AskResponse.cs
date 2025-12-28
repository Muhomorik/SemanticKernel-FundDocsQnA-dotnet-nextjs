namespace Backend.API.Models;

/// <summary>
/// Response model for the /api/ask endpoint.
/// </summary>
public class AskResponse
{
    /// <summary>
    /// Gets or sets the generated answer to the question.
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// Gets or sets the source documents that were used to generate the answer.
    /// </summary>
    public required List<SourceReference> Sources { get; init; }
}
