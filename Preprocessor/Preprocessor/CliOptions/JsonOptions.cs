using CommandLine;

namespace Preprocessor.CliOptions;

/// <summary>
/// CLI options for the 'json' verb - generates embeddings and saves to local JSON file.
/// </summary>
[Verb("json", HelpText = "Generate embeddings and save to local JSON file (embeddings.json)")]
public class JsonOptions : BaseEmbeddingOptions
{
    [Option('o', "output", Required = false, Default = "./embeddings.json",
        HelpText = "Output JSON file path for embeddings")]
    public string Output { get; init; } = "./embeddings.json";

    [Option('a', "append", Required = false, Default = false,
        HelpText = "Append to existing output file instead of overwriting")]
    public bool Append { get; init; }

    /// <summary>
    /// Validates the options and returns any validation errors.
    /// </summary>
    public override IEnumerable<string> Validate()
    {
        foreach (var error in base.Validate())
        {
            yield return error;
        }

        var outputDir = Path.GetDirectoryName(Output);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            yield return $"Output directory does not exist: {outputDir}";
        }
    }
}
