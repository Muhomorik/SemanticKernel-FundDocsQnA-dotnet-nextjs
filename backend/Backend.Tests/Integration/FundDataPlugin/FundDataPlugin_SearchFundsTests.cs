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
/// SK integration tests for <see cref="FundDataPluginImpl.SearchFundsAsync"/>.
/// Tests 3 example queries from the plan document.
/// </summary>
[TestFixture]
[Explicit("Requires OpenAI API key")]
[Category("Integration")]
[Category("FundData.Plugin")]
public class FundDataPlugin_SearchFundsTests
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

        var dbFactory = new TestFundDataDbContextFactory("search-test");
        using var ctx = dbFactory.CreateDbContext();
        ctx.FundProfiles.AddRange(
            // Low-risk passive index fund with good sustainability — should match query 1
            new FundProfile
            {
                Id = IsinId.Create("SE0000000101"),
                Name = "Avanza Zero Index Fund",
                Category = "Equity",
                ManagedType = "PASSIVE",
                IsIndexFund = true,
                Risk = 3,
                ManagementFee = 0.0m,
                TotalFee = 0.0m,
                SustainabilityRating = 4,
                EuArticleType = "Article 8",
                FirstSeenAt = DateTimeOffset.UtcNow
            },
            // Article 9 sustainable fund — should match query 2
            new FundProfile
            {
                Id = IsinId.Create("LU0000000201"),
                Name = "Nordea Global Climate Fund",
                Category = "Equity",
                ManagedType = "ACTIVE",
                Risk = 5,
                ManagementFee = 0.015m,
                TotalFee = 0.018m,
                SustainabilityRating = 5,
                EuArticleType = "Article 9",
                FirstSeenAt = DateTimeOffset.UtcNow
            },
            // Cheap active fund with low risk — should match query 3
            new FundProfile
            {
                Id = IsinId.Create("SE0000000301"),
                Name = "Handelsbanken Active Bond Fund",
                Category = "Fixed Income",
                ManagedType = "ACTIVE",
                Risk = 2,
                ManagementFee = 0.005m,
                TotalFee = 0.007m,
                SustainabilityRating = 3,
                EuArticleType = "Article 8",
                FirstSeenAt = DateTimeOffset.UtcNow
            },
            // High-risk active fund — should NOT match queries 1 or 3
            new FundProfile
            {
                Id = IsinId.Create("SE0000000401"),
                Name = "Aggressive Growth Fund",
                Category = "Equity",
                ManagedType = "ACTIVE",
                Risk = 7,
                ManagementFee = 0.025m,
                TotalFee = 0.030m,
                SustainabilityRating = 2,
                EuArticleType = "Article 6",
                FirstSeenAt = DateTimeOffset.UtcNow
            }
        );
        ctx.SaveChanges();

        var plugin = new FundDataPluginImpl(dbFactory);

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(options.OpenAIChatModel, options.OpenAIApiKey);
        _kernel = builder.Build();
        _kernel.Plugins.AddFromObject(plugin, "FundData");

        _chat = _kernel.GetRequiredService<IChatCompletionService>();
    }

    [Test]
    public async Task ShowMeLowRiskPassiveIndexFunds_ReturnsAvanzaZero()
    {
        // Arrange
        var history = CreateChatHistory("Show me low-risk passive index funds with good sustainability ratings");

        // Act
        var answer = await GetAnswer(history);

        // Assert — should find Avanza Zero (passive, risk 3, sustainability 4)
        Assert.That(answer, Does.Contain("Avanza").IgnoreCase);
        // Should NOT include the high-risk aggressive fund
        Assert.That(answer, Does.Not.Contain("Aggressive").IgnoreCase);
    }

    [Test]
    public async Task WhatArticle9FundsAreAvailable_ReturnsNordeaClimate()
    {
        // Arrange
        var history = CreateChatHistory("What Article 9 funds are available?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — should find Nordea Global Climate (the only Article 9 fund)
        Assert.That(answer, Does.Contain("Nordea").IgnoreCase.Or.Contain("Climate").IgnoreCase);
    }

    [Test]
    public async Task FindCheapActivelyManagedFundsWithLowRisk_ReturnsHandelsbankenBond()
    {
        // Arrange
        var history = CreateChatHistory("Find me cheap actively managed funds with risk level below 4");

        // Act
        var answer = await GetAnswer(history);

        // Assert — should find Handelsbanken Active Bond (active, risk 2, cheap fees)
        Assert.That(answer, Does.Contain("Handelsbanken").IgnoreCase.Or.Contain("Bond").IgnoreCase);
        // Should NOT include the expensive high-risk fund
        Assert.That(answer, Does.Not.Contain("Aggressive").IgnoreCase);
    }

    private static ChatHistory CreateChatHistory(string question)
    {
        var history = new ChatHistory();
        history.AddSystemMessage("You are a helpful fund data assistant. Use the available functions to answer questions about funds.");
        history.AddUserMessage(question);
        return history;
    }

    private async Task<string> GetAnswer(ChatHistory history)
    {
        var result = await _chat.GetChatMessageContentAsync(
            history,
            new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() },
            _kernel);

        return result.Content ?? string.Empty;
    }
}
