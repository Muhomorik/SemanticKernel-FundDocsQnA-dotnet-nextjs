using CommandLine;

namespace Preprocessor.CliOptions;

/// <summary>
/// Base class for embedding-related CLI options (shared between json and cosmosdb verbs).
/// </summary>
public abstract class BaseEmbeddingOptions
{
    [Option('i', "input", Required = false, Default = "pdfs",
        HelpText = "Input directory containing PDF files")]
    public string Input { get; init; } = "pdfs";

    [Option('p', "provider", Required = false,
        HelpText = "Embedding provider: 'ollama', 'lmstudio', or 'openai' (default: openai)")]
    public EmbeddingProvider Provider { get; init; } = EmbeddingProvider.OpenAI;

    [Option("embedding-model", Required = false, Default = "text-embedding-3-small",
        HelpText = "Embedding model for generating embeddings")]
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    [Option("ollama-url", Required = false, Default = null,
        HelpText = "Provider endpoint URL (default: http://localhost:1234 for LMStudio, http://localhost:11434 for Ollama)")]
    public string? OllamaUrl { get; init; }

    [Option("openai-api-key", Required = false, Default = null,
        HelpText = "OpenAI API key (or set OPENAI_API_KEY environment variable)")]
    public string? OpenAIApiKey { get; init; }

    /// <summary>
    /// Gets the effective endpoint URL based on provider and explicit URL.
    /// </summary>
    public string EffectiveUrl => OllamaUrl ?? Provider switch
    {
        EmbeddingProvider.Ollama => "http://localhost:11434",
        EmbeddingProvider.LMStudio => "http://localhost:1234",
        EmbeddingProvider.OpenAI => "https://api.openai.com/v1",
        _ => "http://localhost:1234"
    };

    /// <summary>
    /// Validates the options and returns any validation errors.
    /// </summary>
    public virtual IEnumerable<string> Validate()
    {
        if (!Directory.Exists(Input))
        {
            yield return $"Input directory does not exist: {Input}";
        }

        if (!Uri.TryCreate(EffectiveUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            yield return
                $"Invalid endpoint URL format '{EffectiveUrl}'. Must be a valid HTTP/HTTPS URL. " +
                $"Use --ollama-url to override the default for {Provider}.";
        }

        // Validate OpenAI API key if provided
        if (Provider == EmbeddingProvider.OpenAI)
        {
            if (!string.IsNullOrWhiteSpace(OpenAIApiKey) && !OpenAIApiKey.StartsWith("sk-"))
            {
                yield return
                    $"OpenAI API key appears to be invalid (should start with 'sk-'). " +
                    $"Please check your API key from https://platform.openai.com/api-keys";
            }
        }
    }
}
