namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// Contains the pre-computed sources and a lazy answer stream for SSE delivery.
/// Sources are available immediately (from semantic search); the answer streams token-by-token from the LLM.
/// </summary>
public record StreamingAnswerContext
{
    public required IReadOnlyList<SourceReferenceDto> Sources { get; init; }
    public required IAsyncEnumerable<string> AnswerStream { get; init; }
}
