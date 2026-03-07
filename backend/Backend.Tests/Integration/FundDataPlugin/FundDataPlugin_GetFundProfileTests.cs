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
/// SK integration tests for <see cref="FundDataPluginImpl.GetFundProfileAsync"/>.
/// Tests 3 example queries from the plan document.
/// </summary>
[TestFixture]
public class FundDataPlugin_GetFundProfileTests
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

        var dbFactory = new TestFundDataDbContextFactory("profile-test");
        using var ctx = dbFactory.CreateDbContext();
        ctx.FundProfiles.AddRange(
            new FundProfile
            {
                Id = IsinId.Create("SE0008613939"),
                Name = "Spiltan Globalfond",
                Category = "Equity",
                CompanyName = "Spiltan Fonder",
                ManagedType = "ACTIVE",
                Risk = 5,
                ManagementFee = 0.0125m,
                TotalFee = 0.014m,
                EsgScore = 7.5m,
                SustainabilityRating = 4,
                SustainabilityLevel = "BETTER",
                EnvironmentalScore = 6.2m,
                SocialScore = 8.1m,
                GovernanceScore = 7.9m,
                EuArticleType = "Article 8",
                NumberOfOwners = 125000,
                Capital = 50_000_000m,
                Rating = 4,
                CurrencyCode = "SEK",
                FirstSeenAt = DateTimeOffset.UtcNow
            },
            new FundProfile
            {
                Id = IsinId.Create("LU0274208692"),
                Name = "SEB Emerging Markets Fund",
                Category = "Emerging Markets",
                CompanyName = "SEB Investment Management",
                ManagedType = "ACTIVE",
                Risk = 6,
                ManagementFee = 0.018m,
                TotalFee = 0.021m,
                EsgScore = 5.5m,
                SustainabilityRating = 3,
                NumberOfOwners = 45000,
                Capital = 30_000_000m,
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
    public async Task WhatAreTheFeesForSEBEmergingMarketsFund_ReturnsFeeData()
    {
        // Arrange
        var history = CreateChatHistory("What are the fees for SEB Emerging Markets Fund?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — should mention the management fee (1.8%) or total fee (2.1%)
        Assert.That(answer, Does.Contain("1.8").Or.Contain("0.018").Or.Contain("2.1").Or.Contain("0.021"));
        Assert.That(answer, Does.Contain("SEB").IgnoreCase);
    }

    [Test]
    public async Task WhatsTheEsgScoreForSpiltanGlobalfond_ReturnsEsgData()
    {
        // Arrange
        var history = CreateChatHistory("What's the ESG score and sustainability rating for Spiltan Globalfond?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — should mention ESG score (7.5) and sustainability rating (4)
        Assert.That(answer, Does.Contain("7.5").Or.Contain("7,5"));
        Assert.That(answer, Does.Contain("4"));
        Assert.That(answer, Does.Contain("Spiltan").IgnoreCase);
    }

    [Test]
    public async Task TellMeEverythingAboutIsin_ReturnsFullProfile()
    {
        // Arrange
        var history = CreateChatHistory("Tell me everything about SE0008613939");

        // Act
        var answer = await GetAnswer(history);

        // Assert — should contain key fund details
        Assert.That(answer, Does.Contain("Spiltan").IgnoreCase);
        Assert.That(answer, Does.Contain("Equity").IgnoreCase.Or.Contain("equity").IgnoreCase);
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
