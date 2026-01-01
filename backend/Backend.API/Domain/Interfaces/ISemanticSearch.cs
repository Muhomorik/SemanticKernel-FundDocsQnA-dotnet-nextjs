using Backend.API.Domain.Models;

namespace Backend.API.Domain.Interfaces;

/// <summary>
/// Domain interface for semantic search operations.
/// </summary>
public interface ISemanticSearch
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default);
}
