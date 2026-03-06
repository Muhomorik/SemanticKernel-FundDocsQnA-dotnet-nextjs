using AutoFixture;
using AutoFixture.AutoMoq;

using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Backend.Tests.TestInfrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using FundDataPluginImpl = Backend.API.Infrastructure.FundData.Plugins.FundDataPlugin;

namespace Backend.Tests.Integration.FundDataPlugin;

/// <summary>
/// SK integration tests for <see cref="FundDataPluginImpl.GetTopPerformingFundsAsync"/>.
/// Tests 3 example queries from the plan document.
/// </summary>
[TestFixture]
public class FundDataPlugin_GetTopPerformingFundsTests
{
    private IFixture _fixture = null!;
    private Kernel _kernel = null!;
    private IChatCompletionService _chat = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new BackendDomainCustomization());

        var config = new ConfigurationBuilder()
            .AddUserSecrets<FundDataPlugin_GetTopPerformingFundsTests>()
            .Build();

        var apiKey = config["BackendOptions:OpenAIApiKey"]
                     ?? throw new InvalidOperationException(
                         "OpenAI API key not configured. " +
                         "Set via: cd backend/Backend.Tests && " +
                         "dotnet user-secrets set 'BackendOptions:OpenAIApiKey' 'sk-...'");

        var dbFactory = new TestFundDataDbContextFactory("performance-test");
        using var ctx = dbFactory.CreateDbContext();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Fund A: Big winner (+20% over 30 days), Technology category
        var fundA = new FundProfile
        {
            Id = IsinId.Create("SE0000001001"),
            Name = "Tech Growth Alpha Fund",
            Category = "Technology",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        // Fund B: Moderate gain (+5%), Emerging Markets
        var fundB = new FundProfile
        {
            Id = IsinId.Create("SE0000001002"),
            Name = "EM Steady Growth Fund",
            Category = "Emerging Markets",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        // Fund C: Big loser (-15%), Equity
        var fundC = new FundProfile
        {
            Id = IsinId.Create("SE0000001003"),
            Name = "Declining Value Fund",
            Category = "Equity",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        ctx.FundProfiles.AddRange(fundA, fundB, fundC);

        // Seed NAV history — daily records for last 35 days
        long recordId = 1;
        for (var i = 35; i >= 0; i--)
        {
            var date = today.AddDays(-i);

            // Fund A: starts at 100, ends at 120 (+20%)
            ctx.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.Create(recordId++),
                IsinId = fundA.Id,
                Nav = 100m + (35 - i) * 20m / 35m,
                NavDate = date
            });

            // Fund B: starts at 200, ends at 210 (+5%)
            ctx.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.Create(recordId++),
                IsinId = fundB.Id,
                Nav = 200m + (35 - i) * 10m / 35m,
                NavDate = date
            });

            // Fund C: starts at 150, ends at 127.5 (-15%)
            ctx.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.Create(recordId++),
                IsinId = fundC.Id,
                Nav = 150m - (35 - i) * 22.5m / 35m,
                NavDate = date
            });
        }

        ctx.SaveChanges();

        var plugin = new FundDataPluginImpl(dbFactory);

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion("gpt-4o-mini", apiKey);
        _kernel = builder.Build();
        _kernel.Plugins.AddFromObject(plugin, "FundData");

        _chat = _kernel.GetRequiredService<IChatCompletionService>();
    }

    [Test]
    public async Task TopBestPerformingFundsThisYear_ReturnsTechGrowthFirst()
    {
        // Arrange
        var history = CreateChatHistory("What are the top 5 best performing funds this year?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — Tech Growth Alpha should be the top performer (+20%)
        Assert.That(answer, Does.Contain("Tech Growth").IgnoreCase);
    }

    [Test]
    public async Task WhichFundsLostMostValueLast30Days_ReturnsDecliningFund()
    {
        // Arrange
        var history = CreateChatHistory("Which funds lost the most value in the last 30 days?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — Declining Value Fund should appear as the worst performer (-15%)
        Assert.That(answer, Does.Contain("Declining").IgnoreCase);
    }

    [Test]
    public async Task BestPerformingTechnologyFundsThisWeek_ReturnsTechFund()
    {
        // Arrange
        var history = CreateChatHistory("Best performing technology funds this week?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — Tech Growth Alpha is the only Technology fund
        Assert.That(answer, Does.Contain("Tech Growth").IgnoreCase);
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
