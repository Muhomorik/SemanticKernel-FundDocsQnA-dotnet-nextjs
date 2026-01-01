using Backend.API.ApplicationCore.Configuration;
using Backend.API.ApplicationCore.DTOs;
using Backend.API.Domain.Interfaces;

namespace Backend.API.ApplicationCore.Services;

/// <summary>
/// RAG pipeline orchestration service.
/// Coordinates semantic search and LLM generation.
/// </summary>
public class QuestionAnsweringService : IQuestionAnsweringService
{
    private readonly ISemanticSearch _semanticSearch;
    private readonly ILlmProvider _llmProvider;
    private readonly ApplicationOptions _options;
    private readonly ILogger<QuestionAnsweringService> _logger;
    private readonly IUserQuestionSanitizer _questionSanitizer;

    public QuestionAnsweringService(
        ISemanticSearch semanticSearch,
        ILlmProvider llmProvider,
        ApplicationOptions options,
        ILogger<QuestionAnsweringService> logger,
        IUserQuestionSanitizer questionSanitizer)
    {
        _semanticSearch = semanticSearch;
        _llmProvider = llmProvider;
        _options = options;
        _logger = logger;
        _questionSanitizer = questionSanitizer;
    }

    public async Task<AskQuestionResponse> AnswerQuestionAsync(
        AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Sanitize the question input
            var sanitizedQuestion = _questionSanitizer.Sanitize(request.Question);
            _logger.LogDebug("Question sanitized (original: {Original} chars, result: {Result} chars)",
                request.Question.Length, sanitizedQuestion.Length);

            // Alert on significant removals (potential attack detected)
            if (sanitizedQuestion.Length < request.Question.Length * 0.9)
            {
                _logger.LogWarning(
                    "Sanitization removed {Percent:P1} of input. Possible injection attempt detected. Preview: {Preview}",
                    1 - (double)sanitizedQuestion.Length / request.Question.Length,
                    request.Question.Substring(0, Math.Min(50, request.Question.Length)));
            }

            _logger.LogInformation("Processing sanitized question");

            // Step 1: Semantic search for relevant chunks
            var searchResults = await _semanticSearch.SearchAsync(
                sanitizedQuestion,
                _options.MaxSearchResults,
                cancellationToken);

            if (!searchResults.Any())
            {
                _logger.LogWarning("No relevant chunks found for question");
                return new AskQuestionResponse
                {
                    Answer = "I don't have enough information to answer this question.",
                    Sources = Array.Empty<SourceReferenceDto>()
                };
            }

            _logger.LogDebug("Found {Count} relevant chunks", searchResults.Count);

            // Step 2: Build context from search results
            var context = BuildContext(searchResults);

            // Step 3: Generate answer using LLM
            var userPrompt = GetUserPrompt(context, request.Question);

            _logger.LogDebug("Calling {Provider} LLM with context from {ChunkCount} chunks",
                _llmProvider.ProviderName, searchResults.Count);

            var answer = await _llmProvider.GenerateChatCompletionAsync(
                _options.SystemPrompt,
                userPrompt,
                cancellationToken);

            _logger.LogInformation("Generated answer (length: {Length})", answer.Length);

            // Step 4: Extract unique sources
            var sources = ExtractSources(searchResults);

            return new AskQuestionResponse
            {
                Answer = answer,
                Sources = sources
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to answer question: {Question}", request.Question);
            throw;
        }
    }

    /// <summary>
    /// Builds XML-formatted context from search results for LLM consumption.
    /// </summary>
    /// <param name="results">Search results to format</param>
    /// <returns>XML-formatted context string with chunk tags, source, page, and content</returns>
    /// <remarks>
    /// This method wraps search results in XML tags to provide structured context to the LLM.
    /// XML delimiters prevent prompt injection by making it clear where retrieved context ends
    /// and the user's question begins. Used in conjunction with <see cref="GetUserPrompt"/>
    /// and the system prompt from <see cref="Backend.API.ApplicationCore.Configuration.SystemPromptFactory"/>.
    /// </remarks>
    private string BuildContext(IReadOnlyList<Domain.Models.SearchResult> results)
    {
        var chunks = results.Select((r, idx) =>
            $"<chunk id=\"{idx + 1}\">\n" +
            $"<source>{r.Chunk.Metadata.Source}</source>\n" +
            $"<page>{r.Chunk.Metadata.Page}</page>\n" +
            $"<content>{r.Chunk.Text}</content>\n" +
            $"</chunk>");

        return string.Join("\n\n", chunks);
    }

    private IReadOnlyList<SourceReferenceDto> ExtractSources(
        IReadOnlyList<Domain.Models.SearchResult> results)
    {
        return results
            .Select(r => new SourceReferenceDto
            {
                File = r.Chunk.Metadata.Source,
                Page = r.Chunk.Metadata.Page
            })
            .DistinctBy(s => new { s.File, s.Page })
            .ToList();
    }

    /// <summary>
    /// Constructs the user prompt with XML-delimited context and question for LLM processing.
    /// </summary>
    /// <param name="context">XML-formatted context from search results (see <see cref="BuildContext"/>)</param>
    /// <param name="sanitizedQuestion">User question after sanitization (see <see cref="AnswerQuestionAsync"/>)</param>
    /// <returns>Formatted prompt string with delimited context, question, and instructions</returns>
    /// <remarks>
    /// This method combines the retrieved context and user question with explicit XML tags to create
    /// clear boundaries between system-provided information and user input. This structure is designed
    /// to work with the hardened system prompt from <seealso cref="Backend.API.ApplicationCore.Configuration.SystemPromptFactory"/>
    /// to prevent prompt injection attacks by making it unambiguous where each section begins and ends.
    /// The XML delimiters allow the LLM to distinguish between legitimate retrieved knowledge and
    /// potentially malicious instructions embedded in user input.
    /// </remarks>
    private static string GetUserPrompt(string context, string sanitizedQuestion) =>
        $@"<retrieved_context>
{context}
</retrieved_context>

<user_question>
{sanitizedQuestion}
</user_question>

Answer the user's question based ONLY on the retrieved context above. Do not use external knowledge.";
}