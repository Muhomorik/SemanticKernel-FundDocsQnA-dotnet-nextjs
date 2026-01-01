using Backend.API.Domain.ValueObjects;

namespace Backend.API.Domain.Models;

/// <summary>
/// Domain model representing an answer to a question with supporting evidence.
/// </summary>
public class QuestionAnswer
{
    public string Answer { get; init; }
    public IReadOnlyList<DocumentMetadata> Sources { get; init; }

    public QuestionAnswer(string answer, IEnumerable<DocumentMetadata> sources)
    {
        Answer = answer ?? throw new ArgumentNullException(nameof(answer));
        Sources = sources?.ToList() ?? new List<DocumentMetadata>();
    }
}
