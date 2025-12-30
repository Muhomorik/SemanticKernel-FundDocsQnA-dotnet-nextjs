namespace Backend.API.Infrastructure.LLM.Configuration;

/// <summary>
/// OpenAI configuration extracted from BackendOptions.
/// Maintains backward compatibility.
/// </summary>
public class OpenAiConfiguration : IOpenAiConfiguration
{
    public string ApiKey { get; init; } = string.Empty;
    public string ChatModel { get; init; } = "gpt-4o-mini";
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    public static OpenAiConfiguration FromBackendOptions(Backend.API.Configuration.BackendOptions options)
    {
        return new OpenAiConfiguration
        {
            ApiKey = options.OpenAIApiKey,
            ChatModel = options.OpenAIChatModel,
            EmbeddingModel = options.OpenAIEmbeddingModel
        };
    }
}
