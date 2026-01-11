using CommandLine;

namespace Preprocessor.CliOptions;

/// <summary>
/// CLI options for the 'cosmosdb' verb - generates embeddings and uploads to backend API (Cosmos DB).
/// </summary>
[Verb("cosmosdb", HelpText = "Generate embeddings and upload to backend API (Cosmos DB)")]
public class CosmosDbOptions : BaseEmbeddingOptions
{
    [Option('u', "url", Required = false, Default = "http://localhost:5000",
        HelpText = "Backend API URL")]
    public string Url { get; init; } = "http://localhost:5000";

    [Option('k', "key", Required = false, Default = null,
        HelpText = "API key for backend authentication (or set FUNDDOCS_API_KEY environment variable)")]
    public string? ApiKey { get; init; }

    [Option('o', "operation", Required = false, Default = "add",
        HelpText = "Operation: 'add' (default), 'update', 'replace-all'")]
    public string Operation { get; init; } = "add";

    [Option('b', "batch-size", Required = false, Default = 100,
        HelpText = "Number of embeddings per API request (default: 100)")]
    public int BatchSize { get; init; } = 100;

    /// <summary>
    /// Gets the effective API key from CLI argument or environment variable.
    /// </summary>
    public string? EffectiveApiKey => ApiKey ?? Environment.GetEnvironmentVariable("FUNDDOCS_API_KEY");

    /// <summary>
    /// Validates the options and returns any validation errors.
    /// </summary>
    public override IEnumerable<string> Validate()
    {
        foreach (var error in base.Validate())
        {
            yield return error;
        }

        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            yield return $"Invalid backend URL format '{Url}'. Must be a valid HTTP/HTTPS URL.";
        }

        if (string.IsNullOrWhiteSpace(EffectiveApiKey))
        {
            yield return "API key is required. Set via --key argument or FUNDDOCS_API_KEY environment variable.";
        }

        var validOperations = new[] { "add", "update", "replace-all" };
        if (!validOperations.Contains(Operation.ToLowerInvariant()))
        {
            yield return $"Invalid operation '{Operation}'. Must be one of: {string.Join(", ", validOperations)}";
        }

        if (BatchSize < 1 || BatchSize > 1000)
        {
            yield return $"Batch size must be between 1 and 1000. Got: {BatchSize}";
        }
    }
}
