using Backend.API.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backend.API.Infrastructure.LLM.Providers;

/// <summary>
/// Factory for creating appropriate LLM provider based on configuration.
/// </summary>
public class LlmProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Backend.API.Configuration.BackendOptions _options;
    private readonly ILogger<LlmProviderFactory> _logger;

    public LlmProviderFactory(
        IServiceProvider serviceProvider,
        Backend.API.Configuration.BackendOptions options,
        ILogger<LlmProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    public ILlmProvider CreateProvider()
    {
        _logger.LogInformation("Creating LLM provider: {Provider}", _options.LlmProvider);

        return _options.LlmProvider switch
        {
            Backend.API.Configuration.LlmProvider.OpenAI => _serviceProvider.GetRequiredService<OpenAiProvider>(),
            Backend.API.Configuration.LlmProvider.Groq => _serviceProvider.GetRequiredService<GroqProvider>(),
            _ => throw new InvalidOperationException(
                $"Unknown LLM provider: {_options.LlmProvider}")
        };
    }
}
