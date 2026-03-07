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
/// SK integration tests for <see cref="FundDataPluginImpl.GetFundsByOwnerChangeAsync"/>.
/// Tests 2 example queries from the plan document.
/// </summary>
[TestFixture]
public class FundDataPlugin_GetFundsByOwnerChangeTests
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

        var dbFactory = new TestFundDataDbContextFactory("owner-change-test");
        using var ctx = dbFactory.CreateDbContext();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Fund A: Losing owners (-500), Equity
        var fundA = new FundProfile
        {
            Id = IsinId.Create("SE0000002001"),
            Name = "Unpopular Equity Fund",
            Category = "Equity",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        // Fund B: Gaining owners (+1200), Emerging Markets — biggest gainer
        var fundB = new FundProfile
        {
            Id = IsinId.Create("SE0000002002"),
            Name = "Hot Emerging Markets Fund",
            Category = "Emerging Markets",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        // Fund C: Slight gain (+50), Equity
        var fundC = new FundProfile
        {
            Id = IsinId.Create("SE0000002003"),
            Name = "Stable Equity Fund",
            Category = "Equity",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        ctx.FundProfiles.AddRange(fundA, fundB, fundC);

        // Seed sparse ownership data — only a few data points (realistic)
        long recordId = 1;

        // Fund A: 10000 → 9500 (lost 500)
        ctx.FundHistoryRecords.Add(new FundHistoryRecord
        {
            Id = FundHistoryRecordId.Create(recordId++),
            IsinId = fundA.Id,
            NumberOfOwners = 10000,
            NavDate = today.AddDays(-5)
        });
        ctx.FundHistoryRecords.Add(new FundHistoryRecord
        {
            Id = FundHistoryRecordId.Create(recordId++),
            IsinId = fundA.Id,
            NumberOfOwners = 9500,
            NavDate = today.AddDays(-1)
        });

        // Fund B: 5000 → 6200 (gained 1200)
        ctx.FundHistoryRecords.Add(new FundHistoryRecord
        {
            Id = FundHistoryRecordId.Create(recordId++),
            IsinId = fundB.Id,
            NumberOfOwners = 5000,
            NavDate = today.AddDays(-5)
        });
        ctx.FundHistoryRecords.Add(new FundHistoryRecord
        {
            Id = FundHistoryRecordId.Create(recordId++),
            IsinId = fundB.Id,
            NumberOfOwners = 6200,
            NavDate = today.AddDays(-1)
        });

        // Fund C: 20000 → 20050 (gained 50)
        ctx.FundHistoryRecords.Add(new FundHistoryRecord
        {
            Id = FundHistoryRecordId.Create(recordId++),
            IsinId = fundC.Id,
            NumberOfOwners = 20000,
            NavDate = today.AddDays(-5)
        });
        ctx.FundHistoryRecords.Add(new FundHistoryRecord
        {
            Id = FundHistoryRecordId.Create(recordId++),
            IsinId = fundC.Id,
            NumberOfOwners = 20050,
            NavDate = today.AddDays(-1)
        });

        ctx.SaveChanges();

        var plugin = new FundDataPluginImpl(dbFactory);

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(options.OpenAIChatModel, options.OpenAIApiKey);
        _kernel = builder.Build();
        _kernel.Plugins.AddFromObject(plugin, "FundData");

        _chat = _kernel.GetRequiredService<IChatCompletionService>();
    }

    [Test]
    public async Task WhichFundsArePeopleSellingMost_ReturnsUnpopularFund()
    {
        // Arrange
        var history = CreateChatHistory("Which funds are people selling the most right now?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — Unpopular Equity Fund lost 500 owners
        Assert.That(answer, Does.Contain("Unpopular").IgnoreCase);
    }

    [Test]
    public async Task WhatEmergingMarketsFundGainedMostInvestors_ReturnsHotEM()
    {
        // Arrange
        var history = CreateChatHistory("What emerging markets fund gained the most new investors this month?");

        // Act
        var answer = await GetAnswer(history);

        // Assert — Hot Emerging Markets Fund gained 1200 owners
        Assert.That(answer, Does.Contain("Hot Emerging").IgnoreCase.Or.Contain("Emerging Markets Fund").IgnoreCase);
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
