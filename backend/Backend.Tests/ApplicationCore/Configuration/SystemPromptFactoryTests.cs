using Backend.API.ApplicationCore.Configuration;
using Backend.API.Configuration;

namespace Backend.Tests.ApplicationCore.Configuration;

[TestFixture]
public class SystemPromptFactoryTests
{
    [Test]
    public void Create_NoCustomPrompt_ContainsFunctionCallingInstructions()
    {
        // Arrange
        var options = new BackendOptions
        {
            EmbeddingsFilePath = "",
            OpenAIApiKey = "",
            OpenAIEmbeddingModel = "",
            MemoryCollectionName = ""
        };

        // Act
        var prompt = SystemPromptFactory.Create(options);

        // Assert — hybrid prompt describes both data sources
        Assert.That(prompt, Does.Contain("FUND DATABASE").IgnoreCase);
        Assert.That(prompt, Does.Contain("functions").IgnoreCase);
        Assert.That(prompt, Does.Contain("FUND DOCUMENTS").IgnoreCase);
        Assert.That(prompt, Does.Contain("<retrieved_context>").IgnoreCase);
        Assert.That(prompt, Does.Contain("hybrid").IgnoreCase);
    }

    [Test]
    public void Create_NoCustomPrompt_ContainsSecurityInstructions()
    {
        // Arrange
        var options = new BackendOptions
        {
            EmbeddingsFilePath = "",
            OpenAIApiKey = "",
            OpenAIEmbeddingModel = "",
            MemoryCollectionName = ""
        };

        // Act
        var prompt = SystemPromptFactory.Create(options);

        // Assert — anti-jailbreak rules still present
        Assert.That(prompt, Does.Contain("NEVER role-play"));
        Assert.That(prompt, Does.Contain("DO NOT OVERRIDE"));
        Assert.That(prompt, Does.Contain("Do not make up information"));
    }

    [Test]
    public void Create_CustomPromptConfigured_ReturnsCustomPrompt()
    {
        // Arrange
        var customPrompt = "You are a custom assistant.";
        var options = new BackendOptions
        {
            EmbeddingsFilePath = "",
            OpenAIApiKey = "",
            OpenAIEmbeddingModel = "",
            MemoryCollectionName = "",
            SystemPrompt = customPrompt
        };

        // Act
        var prompt = SystemPromptFactory.Create(options);

        // Assert
        Assert.That(prompt, Is.EqualTo(customPrompt));
    }
}
