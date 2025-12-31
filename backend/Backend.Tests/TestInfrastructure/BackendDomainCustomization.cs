using AutoFixture;
using Backend.API.ApplicationCore.Configuration;
using Backend.API.Configuration;
using Backend.Tests.TestInfrastructure.Builders;

namespace Backend.Tests.TestInfrastructure;

/// <summary>
/// Centralized AutoFixture customization for all backend tests.
/// Configures specimen builders, default values, and mock behaviors.
/// </summary>
public class BackendDomainCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Add custom specimen builders for complex domain objects
        fixture.Customizations.Add(new EmbeddingVectorBuilder());
        fixture.Customizations.Add(new DocumentMetadataBuilder());
        fixture.Customizations.Add(new DocumentChunkBuilder());
        fixture.Customizations.Add(new SearchResultBuilder());
        fixture.Customizations.Add(new AskQuestionRequestBuilder());
        fixture.Customizations.Add(new AskQuestionResponseBuilder());

        // Configure default test values
        fixture.Inject(CreateDefaultApplicationOptions());
        fixture.Inject(CreateDefaultBackendOptions());
    }

    private static ApplicationOptions CreateDefaultApplicationOptions()
    {
        return ApplicationOptions.Create(
            maxSearchResults: 10,
            systemPrompt: "Test system prompt for unit tests"
        );
    }

    private static BackendOptions CreateDefaultBackendOptions()
    {
        return new BackendOptions
        {
            EmbeddingsFilePath = "Data/test-embeddings.json",
            LlmProvider = LlmProvider.OpenAI,
            OpenAIApiKey = "test-openai-key-12345",
            OpenAIEmbeddingModel = "text-embedding-3-small",
            OpenAIChatModel = "gpt-4o-mini",
            MaxSearchResults = 10,
            MemoryCollectionName = "test-collection",
            AllowedOrigins = []
        };
    }
}
