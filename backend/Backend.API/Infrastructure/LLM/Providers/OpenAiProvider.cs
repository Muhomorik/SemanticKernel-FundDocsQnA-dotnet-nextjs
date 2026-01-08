using Backend.API.Domain.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;

namespace Backend.API.Infrastructure.LLM.Providers;

/// <summary>
/// OpenAI implementation of ILlmProvider.
/// Uses Semantic Kernel's IChatCompletionService.
/// Logs token usage for monitoring and cost tracking.
/// </summary>
public class OpenAiProvider : ILlmProvider
{
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<OpenAiProvider> _logger;

    public OpenAiProvider(
        IChatCompletionService chatService,
        ILogger<OpenAiProvider> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public string ProviderName => "OpenAI";

    public async Task<string> GenerateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating chat completion using OpenAI");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(userPrompt);

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);

        // Log token usage if available in metadata
        if (response.Metadata is not null)
        {
            var inputTokens = response.Metadata.TryGetValue("InputTokenCount", out var inputObj)
                ? Convert.ToInt32(inputObj)
                : 0;
            var outputTokens = response.Metadata.TryGetValue("OutputTokenCount", out var outputObj)
                ? Convert.ToInt32(outputObj)
                : 0;
            var totalTokens = inputTokens + outputTokens;

            if (totalTokens > 0)
            {
                _logger.LogInformation("Chat completion token usage - Input: {InputTokens}, Output: {OutputTokens}, Total: {TotalTokens}", inputTokens, outputTokens, totalTokens);
            }
        }

        return response.Content ?? "No answer generated";
    }
}
