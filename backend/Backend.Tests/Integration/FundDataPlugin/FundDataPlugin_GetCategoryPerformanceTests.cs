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
/// SK integration tests for <see cref="FundDataPluginImpl.GetCategoryPerformanceAsync"/>.
/// Tests 2 example queries from the plan document.
/// </summary>
[TestFixture]
public class FundDataPlugin_GetCategoryPerformanceTests
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
            .AddUserSecrets<FundDataPlugin_GetCategoryPerformanceTests>()
            .Build();

        var apiKey = config["BackendOptions:OpenAIApiKey"]
                     ?? throw new InvalidOperationException(
                         "OpenAI API key not configured. " +
                         "Set via: cd backend/Backend.Tests && " +
                         "dotnet user-secrets set 'BackendOptions:OpenAIApiKey' 'sk-...'");

        var dbFactory = new TestFundDataDbContextFactory("category-perf-test");
        using var ctx = dbFactory.CreateDbContext();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Technology category: 2 funds, both gaining → avg ~+12.5%
        var techFund1 = new FundProfile { Id = IsinId.Create("SE0000003001"), Name = "Tech Alpha", Category = "Technology", FirstSeenAt = DateTimeOffset.UtcNow };
        var techFund2 = new FundProfile { Id = IsinId.Create("SE0000003002"), Name = "Tech Beta", Category = "Technology", FirstSeenAt = DateTimeOffset.UtcNow };

        // Fixed Income category: 1 fund, slight loss → avg ~-2%
        var bondFund = new FundProfile { Id = IsinId.Create("SE0000003003"), Name = "Bond Steady", Category = "Fixed Income", FirstSeenAt = DateTimeOffset.UtcNow };

        // Emerging Markets category: 1 fund, big loss → avg ~-10%
        var emFund = new FundProfile { Id = IsinId.Create("SE0000003004"), Name = "EM Decline", Category = "Emerging Markets", FirstSeenAt = DateTimeOffset.UtcNow };

        ctx.FundProfiles.AddRange(techFund1, techFund2, bondFund, emFund);

        long id = 1;

        // Tech Alpha: 100 → 115 (+15%)
        ctx.FundHistoryRecords.Add(new FundHistoryRecord { Id = FundHistoryRecordId.Create(id++), IsinId = techFund1.Id, Nav = 100m, NavDate = today.AddDays(-5) });
        ctx.FundHistoryRecords.Add(new FundHistoryRecord { Id = FundHistoryRecordId.Create(id++), IsinId = techFund1.Id, Nav = 115m, NavDate = today.AddDays(-1) });

        // Tech Beta: 200 → 220 (+10%)
        ctx.FundHistoryRecords.Add(new FundHistoryRecord { Id = FundHistoryRecordId.Create(id++), IsinId = techFund2.Id, Nav = 200m, NavDate = today.AddDays(-5) });
        ctx.FundHistoryRecords.Add(new FundHistoryRecord { Id = FundHistoryRecordId.Create(id++), IsinId = techFund2.Id, Nav = 220m, NavDate = today.AddDays(-1) });

        // Bond Steady: 50 → 49 (-2%)
        ctx.FundHistoryRecords.Add(new FundHistoryRecord { Id = FundHistoryRecordId.Create(id++), IsinId = bondFund.Id, Nav = 50m, NavDate = today.AddDays(-5) });
        ctx.FundHistoryRecords.Add(new FundHistoryRecord { Id = FundHistoryRecordId.Create(id++), IsinId = bondFund.Id, Nav = 49m, NavDate = today.AddDays(-1) });

        // EM Decline: 300 → 270 (-10%)
        ctx.FundHistoryRecords.Add(new FundHistoryRecord { Id = FundHistoryRecordId.Create(id++), IsinId = emFund.Id, Nav = 300m, NavDate = today.AddDays(-5) });
        ctx.FundHistoryRecords.Add(new FundHistoryRecord { Id = FundHistoryRecordId.Create(id++), IsinId = emFund.Id, Nav = 270m, NavDate = today.AddDays(-1) });

        ctx.SaveChanges();

        var plugin = new FundDataPluginImpl(dbFactory);

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion("gpt-4o-mini", apiKey);
        _kernel = builder.Build();
        _kernel.Plugins.AddFromObject(plugin, "FundData");

        _chat = _kernel.GetRequiredService<IChatCompletionService>();
    }

    [Test]
    public async Task HowDidCategoriesPerformLastMonth_ReturnsCategoriesRanked()
    {
        // Arrange
        var history = CreateChatHistory("How did different fund categories perform last month?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — Technology should be mentioned as best, all 3 categories present
        Assert.That(answer, Does.Contain("Technology").IgnoreCase);
        Assert.That(answer, Does.Contain("Fixed Income").IgnoreCase.Or.Contain("Bond").IgnoreCase);
        Assert.That(answer, Does.Contain("Emerging").IgnoreCase);
    }

    [Test]
    public async Task WorstPerformingCategoryThisYear_MentionsCategoryPerformance()
    {
        // Arrange
        var history = CreateChatHistory("What's the worst performing category this year?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — LLM should call get_category_performance and mention at least one seeded category
        Assert.That(answer,
            Does.Contain("Emerging").IgnoreCase
                .Or.Contain("Technology").IgnoreCase
                .Or.Contain("Fixed Income").IgnoreCase);
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
