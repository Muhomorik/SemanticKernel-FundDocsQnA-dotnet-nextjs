using AutoFixture;

using Backend.API.ApplicationCore.Configuration;
using Backend.API.Configuration;

using Microsoft.Extensions.Configuration;

namespace Backend.Tests.TestInfrastructure;

/// <summary>
/// AutoFixture customization for integration tests that require real OpenAI API keys.
/// Reads configuration from user secrets and injects production-like BackendOptions
/// and ApplicationOptions (with real system prompt via SystemPromptFactory).
/// Apply AFTER BackendDomainCustomization — last Inject wins.
/// </summary>
public class IntegrationTestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<IntegrationTestCustomization>()
            .Build();

        var apiKey = config["BackendOptions:OpenAIApiKey"]
                     ?? throw new InvalidOperationException(
                         "OpenAI API key not configured for integration tests. " +
                         "Set via: cd backend/Backend.Tests && " +
                         "dotnet user-secrets set 'BackendOptions:OpenAIApiKey' 'sk-...'");

        var options = new BackendOptions
        {
            EmbeddingsFilePath = TestDataPaths.TestEmbeddingsJson,
            LlmProvider = LlmProvider.OpenAI,
            OpenAIApiKey = apiKey,
            OpenAIEmbeddingModel = config["BackendOptions:OpenAIEmbeddingModel"] ?? "text-embedding-3-small",
            OpenAIChatModel = config["BackendOptions:OpenAIChatModel"] ?? "gpt-4.1-mini",
            MaxSearchResults = 10,
            MemoryCollectionName = "fund-documents",
            AllowedOrigins = []
        };

        fixture.Inject(options);
        fixture.Inject(ApplicationOptions.Create(options));
    }
}
