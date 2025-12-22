using Backend.API.Configuration;
using Backend.API.Models;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Backend.API.Services;

/// <summary>
/// Service that orchestrates semantic search and LLM generation to answer questions about documents.
/// </summary>
public class QuestionAnsweringService : IQuestionAnsweringService
{
    private readonly IMemoryService _memoryService;
    private readonly IChatCompletionService _chatService;
    private readonly BackendOptions _options;
    private readonly ILogger<QuestionAnsweringService> _logger;

    public QuestionAnsweringService(
        IMemoryService memoryService,
        Kernel kernel,
        BackendOptions options,
        ILogger<QuestionAnsweringService> logger)
    {
        _memoryService = memoryService;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _options = options;
        _logger = logger;
    }

    public async Task<AskResponse> AnswerQuestionAsync(string question, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Answering question: {Question}", question);

            // Step 1: Search for relevant chunks
            var relevantChunks = await _memoryService.SearchAsync(
                question,
                _options.MaxSearchResults,
                cancellationToken);

            if (relevantChunks.Count == 0)
            {
                _logger.LogWarning("No relevant chunks found for question");
                return new AskResponse
                {
                    Answer = "I don't have enough information to answer this question.",
                    Sources = new List<SourceReference>()
                };
            }

            _logger.LogDebug("Found {Count} relevant chunks", relevantChunks.Count);

            // Step 2: Build context from retrieved chunks
            var context = string.Join("\n\n", relevantChunks.Select((chunk, index) =>
                $"[{index + 1}] Source: {chunk.Source}, Page: {chunk.Page}\n{chunk.Text}"));

            // Step 3: Create prompt
            var systemPrompt = @"You are a helpful assistant that answers questions about financial fund documents.

Use the following context to answer the question. If the answer is not in the context, say ""I don't have enough information to answer this question.""

Always base your answer strictly on the provided context. Do not make up information.";

            var userPrompt = $@"Context:
{context}

Question: {question}

Answer:";

            // Step 4: Call Groq LLM
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(userPrompt);

            _logger.LogDebug("Calling Groq LLM with context from {ChunkCount} chunks", relevantChunks.Count);

            var response = await _chatService.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            var answer = response.Content ?? "No answer generated";
            _logger.LogInformation("Generated answer (length: {Length})", answer.Length);

            // Step 5: Extract sources
            var sources = relevantChunks
                .Select(chunk => new SourceReference
                {
                    File = chunk.Source,
                    Page = chunk.Page
                })
                .DistinctBy(s => new { s.File, s.Page })
                .ToList();

            return new AskResponse
            {
                Answer = answer,
                Sources = sources
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to answer question: {Question}", question);
            throw;
        }
    }
}