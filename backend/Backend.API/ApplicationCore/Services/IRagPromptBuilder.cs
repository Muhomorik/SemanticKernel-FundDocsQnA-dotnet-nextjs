using Backend.API.Domain.Models;

namespace Backend.API.ApplicationCore.Services;

/// <summary>
/// Builds RAG prompt components from search results.
/// Formats document chunks into XML-delimited context and assembles user prompts.
/// </summary>
public interface IRagPromptBuilder
{
    /// <summary>
    /// Formats search results as XML chunks for LLM consumption.
    /// </summary>
    string BuildContext(IReadOnlyList<SearchResult> results);

    /// <summary>
    /// Constructs the user prompt with XML-delimited context and question.
    /// </summary>
    string BuildUserPrompt(string context, string question);
}
