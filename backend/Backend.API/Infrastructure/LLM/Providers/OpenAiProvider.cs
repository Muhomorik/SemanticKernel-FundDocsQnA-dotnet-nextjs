using Backend.API.Domain.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;

namespace Backend.API.Infrastructure.LLM.Providers;

/// <summary>
/// OpenAI implementation of ILlmProvider with function-calling support.
/// Uses Semantic Kernel's IChatCompletionService with FunctionChoiceBehavior.Auto(),
/// allowing the LLM to call registered Kernel plugins (e.g. FundDataPlugin) when answering questions.
/// Logs token usage for monitoring and cost tracking.
/// </summary>
public class OpenAiProvider : ILlmProvider
{
    private readonly IChatCompletionService _chatService;
    private readonly Kernel _kernel;
    private readonly ILogger<OpenAiProvider> _logger;

    public OpenAiProvider(
        IChatCompletionService chatService,
        Kernel kernel,
        ILogger<OpenAiProvider> logger)
    {
        _chatService = chatService;
        _kernel = kernel;
        _logger = logger;
    }

    public string ProviderName => "OpenAI";

    public async Task<string> GenerateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating chat completion using OpenAI (plugins: {PluginCount})",
            _kernel.Plugins.Count);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(userPrompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory, settings, _kernel, cancellationToken);

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
