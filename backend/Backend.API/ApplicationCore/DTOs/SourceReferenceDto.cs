namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// Application DTO for source document reference.
/// </summary>
public record SourceReferenceDto
{
    /// <summary>
    /// Gets the source PDF filename.
    /// </summary>
    public required string File { get; init; }

    /// <summary>
    /// Gets the page number in the source PDF.
    /// </summary>
    public required int Page { get; init; }
}
