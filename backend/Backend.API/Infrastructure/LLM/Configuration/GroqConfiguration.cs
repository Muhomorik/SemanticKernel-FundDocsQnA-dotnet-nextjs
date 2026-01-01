namespace Backend.API.Infrastructure.LLM.Configuration;

/// <summary>
/// Groq configuration extracted from BackendOptions.
/// Maintains backward compatibility.
/// </summary>
public class GroqConfiguration : IGroqConfiguration
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "llama-3.3-70b-versatile";
    public string ApiUrl { get; init; } = "https://api.groq.com/openai/v1";

    public static GroqConfiguration FromBackendOptions(Backend.API.Configuration.BackendOptions options)
    {
        return new GroqConfiguration
        {
            ApiKey = options.GroqApiKey ?? string.Empty,
            Model = options.GroqModel ?? "llama-3.3-70b-versatile",
            ApiUrl = options.GroqApiUrl ?? "https://api.groq.com/openai/v1"
        };
    }
}
