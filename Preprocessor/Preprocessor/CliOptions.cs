using CommandLine;
using System.Diagnostics;

namespace Preprocessor;

/// <summary>
/// Command line options for the Preprocessor application.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class CliOptions
{
    [Option('m', "method", Default = "pdfpig", HelpText = "Extraction method: 'pdfpig' or 'ollama-vision'")]
    public string Method { get; init; } = "pdfpig";

    [Option('i', "input", Required = false, Default = "pdfs", HelpText = "Input directory containing PDF files")]
    public string Input { get; init; } = "pdfs";

    [Option('o', "output", Required = false, Default = "output.json",
        HelpText = "Output JSON file path for embeddings")]
    public string Output { get; init; } = "output.json";

    [Option('a', "append", Required = false, Default = false,
        HelpText = "Append to existing output file instead of overwriting")]
    public bool Append { get; init; }

    [Option("vision-model", Required = false, Default = "llava",
        HelpText = "Vision model for ollama-vision extraction")]
    public string VisionModel { get; init; } = "llava";

    [Option("embedding-model", Required = false, Default = "nomic-embed-text",
        HelpText = "Embedding model for generating embeddings")]
    public string EmbeddingModel { get; init; } = "nomic-embed-text";

    [Option("ollama-url", Required = false, Default = "http://localhost:11434", HelpText = "Ollama server URL")]
    public string OllamaUrl { get; init; } = "http://localhost:11434";

    /// <summary>
    /// Validates the options and returns any validation errors.
    /// </summary>
    public IEnumerable<string> Validate()
    {
        var validMethods = new[] { "pdfpig", "ollama-vision" };
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

        if (!Uri.TryCreate(OllamaUrl, UriKind.Absolute, out var ollamaUri) ||
            (ollamaUri.Scheme != Uri.UriSchemeHttp && ollamaUri.Scheme != Uri.UriSchemeHttps))
        {
            yield return $"Invalid Ollama URL format '{OllamaUrl}'. Must be a valid HTTP/HTTPS URL (e.g., http://localhost:11434). Use --ollama-url to specify a different endpoint.";
        }
    }

    private string DebuggerDisplay => $"Method={Method}, Input={Input}, Output={Output}, Append={Append}, VisionModel={VisionModel}";
}