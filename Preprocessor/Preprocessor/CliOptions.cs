using CommandLine;

using System.Diagnostics;

namespace Preprocessor;

/// <summary>
/// Command line options for the Preprocessor application.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class CliOptions
{
    [Option('m', "method", Default = "pdfpig", HelpText = "Extraction method: 'pdfpig'")]
    public string Method { get; init; } = "pdfpig";

    [Option('i', "input", Required = false, Default = "pdfs", HelpText = "Input directory containing PDF files")]
    public string Input { get; init; } = "pdfs";

    [Option('o', "output", Required = false, Default = "./embeddings.json",
        HelpText = "Output JSON file path for embeddings")]
    public string Output { get; init; } = "./embeddings.json";

    [Option('a', "append", Required = false, Default = false,
        HelpText = "Append to existing output file instead of overwriting")]
    public bool Append { get; init; }

    [Option("embedding-model", Required = false, Default = "text-embedding-3-small",
        HelpText = "Embedding model for generating embeddings")]
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    [Option('p', "provider", Required = false, Default = EmbeddingProvider.OpenAI,
        HelpText = "Embedding provider: 'ollama', 'lmstudio', or 'openai' (default: openai)")]
    public EmbeddingProvider Provider { get; init; } = EmbeddingProvider.OpenAI;

    [Option("ollama-url", Required = false, Default = null,
        HelpText = "Provider endpoint URL (default: http://localhost:1234 for LMStudio, http://localhost:11434 for Ollama)")]
    public string? OllamaUrl { get; init; }

    [Option("openai-api-key", Required = false, Default = null,
        HelpText = "OpenAI API key (or set OPENAI_API_KEY environment variable)")]
    public string? OpenAIApiKey { get; init; }

    /// <summary>
    /// Gets the effective endpoint URL based on provider and explicit URL.
    /// </summary>
    /// <remarks>
    /// <para><strong>Default URLs by Provider:</strong></para>
    /// <list type="bullet">
    ///   <item><description>Ollama: http://localhost:11434</description></item>
    ///   <item><description>LM Studio: http://localhost:1234</description></item>
    ///   <item><description>OpenAI: https://api.openai.com/v1</description></item>
    /// </list>
    /// <para>
    /// You can override the default with --ollama-url. The parameter name is kept for
    /// backward compatibility but works for all providers.
    /// </para>
    /// </remarks>
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
    public IEnumerable<string> Validate()
    {
        var validMethods = new[] { "pdfpig" };
        if (!validMethods.Contains(Method.ToLowerInvariant()))
        {
            yield return $"Invalid method '{Method}'. Must be one of: {string.Join(", ", validMethods)}";
        }

        if (!Directory.Exists(Input))
        {
            yield return $"Input directory does not exist: {Input}";
        }

        var outputDir = Path.GetDirectoryName(Output);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            yield return $"Output directory does not exist: {outputDir}";
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

    private string DebuggerDisplay =>
        $"Method={Method}, Input={Input}, Output={Output}, Append={Append}";
}