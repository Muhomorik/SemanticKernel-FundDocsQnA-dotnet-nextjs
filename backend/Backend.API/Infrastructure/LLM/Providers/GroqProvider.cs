using Backend.API.Domain.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;

namespace Backend.API.Infrastructure.LLM.Providers;

/// <summary>
/// Groq implementation of ILlmProvider.
/// Uses Groq API via Semantic Kernel's OpenAI-compatible interface.
/// </summary>
public class GroqProvider : ILlmProvider
{
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<GroqProvider> _logger;

    public string ProviderName => "Groq";

    public GroqProvider(
        IChatCompletionService chatService,
        ILogger<GroqProvider> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task<string> GenerateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating chat completion using Groq");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(userPrompt);

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);

        return response.Content ?? "No answer generated";
    }
}
