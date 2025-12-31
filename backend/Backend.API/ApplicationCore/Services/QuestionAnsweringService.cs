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

    public QuestionAnsweringService(
        ISemanticSearch semanticSearch,
        ILlmProvider llmProvider,
        ApplicationOptions options,
        ILogger<QuestionAnsweringService> logger)
    {
        _semanticSearch = semanticSearch;
        _llmProvider = llmProvider;
        _options = options;
        _logger = logger;
    }

    public async Task<AskQuestionResponse> AnswerQuestionAsync(
        AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Answering question: {Question}", request.Question);

            // Step 1: Semantic search for relevant chunks
            var searchResults = await _semanticSearch.SearchAsync(
                request.Question,
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

    private string BuildContext(IReadOnlyList<Domain.Models.SearchResult> results)
    {
        return string.Join("\n\n", results.Select((r, idx) =>
            $"[{idx + 1}] Source: {r.Chunk.Metadata.Source}, " +
            $"Page: {r.Chunk.Metadata.Page}\n{r.Chunk.Text}"));
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

    private static string GetUserPrompt(string context, string question) =>
        $@"Context:
{context}

Question: {question}

Answer:";
}