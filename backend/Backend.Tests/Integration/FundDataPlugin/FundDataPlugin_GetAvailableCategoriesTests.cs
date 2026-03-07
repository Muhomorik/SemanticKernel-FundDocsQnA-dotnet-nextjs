using AutoFixture;

using Backend.API.Configuration;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Backend.Tests.TestInfrastructure;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using FundDataPluginImpl = Backend.API.Infrastructure.FundData.Plugins.FundDataPlugin;

namespace Backend.Tests.Integration.FundDataPlugin;

/// <summary>
/// SK integration tests for <see cref="FundDataPluginImpl.GetAvailableCategoriesAsync"/>.
/// Validates the full function-calling loop: LLM receives a natural language question,
/// picks the right function, and returns an answer containing seeded category data.
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("FundData.Plugin")]
public class FundDataPlugin_GetAvailableCategoriesTests
{
    private Kernel _kernel = null!;
    private IChatCompletionService _chat = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var fixture = new Fixture()
            .Customize(new BackendDomainCustomization())
            .Customize(new IntegrationTestCustomization());

        var options = fixture.Create<BackendOptions>();

        // Seed test data with specific categories
        var dbFactory = new TestFundDataDbContextFactory("categories-test");
        using var ctx = dbFactory.CreateDbContext();
        ctx.FundProfiles.AddRange(
            CreateProfile("SE0000000001", "Fund Alpha", "Equity"),
            CreateProfile("SE0000000002", "Fund Beta", "Fixed Income"),
            CreateProfile("SE0000000003", "Fund Gamma", "Equity"),
            CreateProfile("SE0000000004", "Fund Delta", null)
        );
        ctx.SaveChanges();

        // Build kernel with plugin + OpenAI
        var plugin = new FundDataPluginImpl(dbFactory);

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(options.OpenAIChatModel, options.OpenAIApiKey);
        _kernel = builder.Build();
        _kernel.Plugins.AddFromObject(plugin, "FundData");

        _chat = _kernel.GetRequiredService<IChatCompletionService>();
    }

    [Test]
    public async Task WhatFundCategoriesAreAvailable_ReturnsSeededCategories()
    {
        // Arrange
        var history = new ChatHistory();
        history.AddSystemMessage("You are a helpful fund data assistant. Use the available functions to answer questions about funds.");
        history.AddUserMessage("What fund categories are available?");

        // Act
        var result = await _chat.GetChatMessageContentAsync(
            history,
            new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() },
            _kernel);

        var answer = result.Content ?? string.Empty;

        // Assert
        Assert.That(answer, Does.Contain("Equity").IgnoreCase);
        Assert.That(answer, Does.Contain("Fixed Income").IgnoreCase);
    }

    private static FundProfile CreateProfile(string isin, string name, string? category) => new()
    {
        Id = IsinId.Create(isin),
        Name = name,
        Category = category,
        FirstSeenAt = DateTimeOffset.UtcNow
    };
}
