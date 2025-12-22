namespace Backend.API.Models;

/// <summary>
/// Represents a reference to a source document.
/// </summary>
public class SourceReference
{
    /// <summary>
    /// Gets or sets the source PDF filename.
    /// </summary>
    public required string File { get; init; }

    /// <summary>
    /// Gets or sets the page number in the source PDF.
    /// </summary>
    public required int Page { get; init; }
}
