using Backend.API.Domain.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;

namespace Backend.API.Infrastructure.LLM.Providers;

/// <summary>
/// OpenAI implementation of ILlmProvider.
/// Uses Semantic Kernel's IChatCompletionService.
/// </summary>
public class OpenAiProvider : ILlmProvider
{
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<OpenAiProvider> _logger;

    public string ProviderName => "OpenAI";

    public OpenAiProvider(
        IChatCompletionService chatService,
        ILogger<OpenAiProvider> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

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

        return response.Content ?? "No answer generated";
    }
}
