using CommandLine;

namespace Preprocessor;

/// <summary>
/// Command line options for the Preprocessor application.
/// </summary>
public class Options
{
    [Option('m', "method", Required = true, HelpText = "Extraction method: 'pdfpig' or 'ollama-vision'")]
    public required string Method { get; init; }

    [Option('i', "input", Required = true, HelpText = "Input directory containing PDF files")]
    public required string Input { get; init; }

    [Option('o', "output", Required = true, HelpText = "Output JSON file path for embeddings")]
    public required string Output { get; init; }

    [Option('a', "append", Required = false, Default = false, HelpText = "Append to existing output file instead of overwriting")]
    public bool Append { get; init; }

    [Option("vision-model", Required = false, Default = "llava", HelpText = "Vision model for ollama-vision extraction")]
    public string VisionModel { get; init; } = "llava";

    [Option("embedding-model", Required = false, Default = "nomic-embed-text", HelpText = "Embedding model for generating embeddings")]
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
    }
}
