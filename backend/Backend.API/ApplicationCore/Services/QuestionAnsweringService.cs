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
    private readonly IRagPromptBuilder _ragPromptBuilder;
    private readonly ApplicationOptions _options;
    private readonly ILogger<QuestionAnsweringService> _logger;
    private readonly IUserQuestionSanitizer _questionSanitizer;

    public QuestionAnsweringService(
        ISemanticSearch semanticSearch,
        ILlmProvider llmProvider,
        IRagPromptBuilder ragPromptBuilder,
        ApplicationOptions options,
        ILogger<QuestionAnsweringService> logger,
        IUserQuestionSanitizer questionSanitizer)
    {
        _semanticSearch = semanticSearch;
        _llmProvider = llmProvider;
        _ragPromptBuilder = ragPromptBuilder;
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
            var context = _ragPromptBuilder.BuildContext(searchResults);

            // Step 3: Generate answer using LLM
            var userPrompt = _ragPromptBuilder.BuildUserPrompt(context, request.Question);

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

    public async Task<StreamingAnswerContext> BeginStreamingAnswerAsync(
        AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Sanitize the question input
            var sanitizedQuestion = _questionSanitizer.Sanitize(request.Question);
            _logger.LogDebug("Question sanitized for streaming (original: {Original} chars, result: {Result} chars)",
                request.Question.Length, sanitizedQuestion.Length);

            if (sanitizedQuestion.Length < request.Question.Length * 0.9)
            {
                _logger.LogWarning(
                    "Sanitization removed {Percent:P1} of input. Possible injection attempt detected. Preview: {Preview}",
                    1 - (double)sanitizedQuestion.Length / request.Question.Length,
                    request.Question.Substring(0, Math.Min(50, request.Question.Length)));
            }

            _logger.LogInformation("Processing sanitized question (streaming)");

            // Step 1: Semantic search for relevant chunks
            var searchResults = await _semanticSearch.SearchAsync(
                sanitizedQuestion,
                _options.MaxSearchResults,
                cancellationToken);

            if (!searchResults.Any())
            {
                _logger.LogWarning("No relevant chunks found for question");
                return new StreamingAnswerContext
                {
                    Sources = Array.Empty<SourceReferenceDto>(),
                    AnswerStream = NoResultsStreamAsync()
                };
            }

            _logger.LogDebug("Found {Count} relevant chunks", searchResults.Count);

            // Step 2: Build context and prompt
            var context = _ragPromptBuilder.BuildContext(searchResults);
            var userPrompt = _ragPromptBuilder.BuildUserPrompt(context, request.Question);
            var sources = ExtractSources(searchResults);

            _logger.LogDebug("Starting streaming from {Provider} LLM with context from {ChunkCount} chunks",
                _llmProvider.ProviderName, searchResults.Count);

            // Step 3: Return sources + lazy answer stream
            var answerStream = _llmProvider.StreamChatCompletionAsync(
                _options.SystemPrompt, userPrompt, cancellationToken);

            return new StreamingAnswerContext
            {
                Sources = sources,
                AnswerStream = answerStream
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin streaming answer for question: {Question}", request.Question);
            throw;
        }
    }

    private static async IAsyncEnumerable<string> NoResultsStreamAsync()
    {
        yield return "I don't have enough information to answer this question.";
        await Task.CompletedTask;
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

}