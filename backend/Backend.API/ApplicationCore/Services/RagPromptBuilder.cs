using Backend.API.Domain.Models;

namespace Backend.API.ApplicationCore.Services;

/// <inheritdoc />
public class RagPromptBuilder : IRagPromptBuilder
{
    /// <inheritdoc />
    public string BuildContext(IReadOnlyList<SearchResult> results)
    {
        var chunks = results.Select((r, idx) =>
            $"<chunk id=\"{idx + 1}\">\n" +
            $"<source>{r.Chunk.Metadata.Source}</source>\n" +
            $"<page>{r.Chunk.Metadata.Page}</page>\n" +
            $"<content>{r.Chunk.Text}</content>\n" +
            $"</chunk>");

        return string.Join("\n\n", chunks);
    }

    /// <inheritdoc />
    public string BuildUserPrompt(string context, string question) =>
        $@"<retrieved_context>
{context}
</retrieved_context>

<user_question>
{question}
</user_question>

Answer the user's question using the retrieved context and any available functions. Do not use external knowledge.";
}
