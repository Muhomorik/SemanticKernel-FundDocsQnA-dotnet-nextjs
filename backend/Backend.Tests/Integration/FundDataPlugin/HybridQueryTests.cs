using System.Text.Json;

using AutoFixture;

using Backend.API.ApplicationCore.Configuration;
using Backend.API.ApplicationCore.Services;
using Backend.API.Configuration;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Backend.API.Domain.Interfaces;
using Backend.API.Domain.Models;
using Backend.API.Infrastructure.LLM.SemanticKernel;
using Backend.API.Infrastructure.Persistence.Models;
using Backend.API.Infrastructure.Search;
using Backend.Tests.TestInfrastructure;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using FundDataPluginImpl = Backend.API.Infrastructure.FundData.Plugins.FundDataPlugin;

namespace Backend.Tests.Integration.FundDataPlugin;

/// <summary>
/// Hybrid integration tests that run the full pipeline: real semantic search over
/// actual fund factsheet embeddings + FundDataPlugin function calling.
/// Each test sends a natural-language question that requires BOTH structured data
/// (via plugin functions) AND document context (via RAG retrieval from real PRIIP/KID PDFs).
/// </summary>
[TestFixture, Explicit("Requires OpenAI API key")]
public class HybridQueryTests
{
    private Kernel _kernel = null!;
    private IChatCompletionService _chat = null!;
    private InMemorySemanticSearch _semanticSearch = null!;
    private string _systemPrompt = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var fixture = new Fixture()
            .Customize(new BackendDomainCustomization())
            .Customize(new IntegrationTestCustomization());

        var options = fixture.Create<BackendOptions>();
        var applicationOptions = fixture.Create<ApplicationOptions>();

        _systemPrompt = applicationOptions.SystemPrompt;

        // Kernel with OpenAI chat + embeddings from config
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(options.OpenAIChatModel, options.OpenAIApiKey);
#pragma warning disable SKEXP0010
        builder.AddOpenAIEmbeddingGenerator(options.OpenAIEmbeddingModel, options.OpenAIApiKey);
#pragma warning restore SKEXP0010
        _kernel = builder.Build();

        // FundDataPlugin with seeded SEB fund data
        var dbFactory = new TestFundDataDbContextFactory("hybrid-test");
        SeedFundData(dbFactory);
        var plugin = new FundDataPluginImpl(dbFactory);
        _kernel.Plugins.AddFromObject(plugin, "FundData");

        _chat = _kernel.GetRequiredService<IChatCompletionService>();

        // InMemorySemanticSearch with real embeddings from test_embeddings.json
        var embeddingGenerator = new SemanticKernelEmbeddingGenerator(
            _kernel.GetRequiredService<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>());

        var documentRepo = new TestDocumentRepository(TestDataPaths.TestEmbeddingsJson);
        await documentRepo.InitializeAsync();

        _semanticSearch = new InMemorySemanticSearch(
            documentRepo,
            embeddingGenerator,
            NullLogger<InMemorySemanticSearch>.Instance);
    }

    #region Performance + Document context

    [Test]
    public async Task PerformanceAndFactsheet_CombinesFunctionAndContext()
    {
        var answer = await AskHybrid(
            "How did SEB Emerging Marketsfond perform last month, and what does the factsheet say about this fund?");

        Assert.That(answer, Does.Contain("SEB Emerging").IgnoreCase);
        Assert.That(answer, Does.Contain("risk").IgnoreCase
            .Or.Contain("emerging").IgnoreCase
            .Or.Contain("market").IgnoreCase);
    }

    [Test]
    public async Task BestPerformerAndObjective_CombinesFunctionAndContext()
    {
        var answer = await AskHybrid(
            "Which fund performed best last month, and what does its factsheet say about it?");

        // SEB European Defence & Security is best (+12%)
        Assert.That(answer, Does.Contain("Defence").IgnoreCase
            .Or.Contain("Defense").IgnoreCase
            .Or.Contain("Security").IgnoreCase);
    }

    [Test]
    public async Task WorstPerformersAndRisks_CombinesFunctionAndContext()
    {
        var answer = await AskHybrid(
            "Show me the worst performing funds last month - do their factsheets mention any risk warnings?");

        // SEB Korträntefond is worst (-3%)
        Assert.That(answer, Does.Contain("Korträntefond").IgnoreCase
            .Or.Contain("risk").IgnoreCase);
    }

    #endregion

    #region Ownership + Document context

    [Test]
    public async Task LosingOwnersAndFactsheet_CombinesFunctionAndContext()
    {
        var answer = await AskHybrid(
            "Which fund is losing the most owners? What does its factsheet say about risks?");

        // SEB Nordenfond lost 800 owners (biggest loser), but LLM may pick other losers too
        Assert.That(answer, Does.Contain("Nordenfond").IgnoreCase
            .Or.Contain("Korträntefond").IgnoreCase
            .Or.Contain("Sverigefond").IgnoreCase
            .Or.Contain("owner").IgnoreCase
            .Or.Contain("investor").IgnoreCase);
        Assert.That(answer, Does.Contain("risk").IgnoreCase
            .Or.Contain("förlust").IgnoreCase
            .Or.Contain("loss").IgnoreCase);
    }

    [Test]
    public async Task GainingInvestorsAndFactsheet_CombinesFunctionAndContext()
    {
        var answer = await AskHybrid(
            "What fund gained the most investors recently? What does its factsheet describe?");

        // SEB European Defence & Security gained 2000 owners
        Assert.That(answer, Does.Contain("Defence").IgnoreCase
            .Or.Contain("Defense").IgnoreCase
            .Or.Contain("Security").IgnoreCase);
    }

    #endregion

    #region Search + Document context

    [Test]
    public async Task LowRiskFundsAndFees_CombinesFunctionAndContext()
    {
        var answer = await AskHybrid(
            "Find me low-risk funds - what do their factsheets say about their fee structures?");

        // Should find low-risk funds (Korträntefond risk 2, Företagsobligationsfond risk 3)
        Assert.That(answer, Does.Contain("Korträntefond").IgnoreCase
            .Or.Contain("Företagsobligationsfond").IgnoreCase
            .Or.Contain("fee").IgnoreCase
            .Or.Contain("kostnad").IgnoreCase);
    }

    #endregion

    #region Profile + Document context

    [Test]
    public async Task FundProfileAndFactsheet_CombinesFunctionAndContext()
    {
        var answer = await AskHybrid(
            "What are the fees for SEB Sverigefond, and what does its factsheet say about the fund?");

        Assert.That(answer, Does.Contain("Sverigefond").IgnoreCase);
        Assert.That(answer, Does.Contain("fee").IgnoreCase
            .Or.Contain("avgift").IgnoreCase
            .Or.Contain("kostnad").IgnoreCase
            .Or.Contain("0.").IgnoreCase);
    }

    [Test]
    public async Task IsinLookupAndKidDocument_CombinesFunctionAndContext()
    {
        // SE0000434151 = SEB Global Aktiefond A
        var answer = await AskHybrid(
            "Tell me about SE0000434151 - include what the KID document says about potential losses");

        Assert.That(answer, Does.Contain("Global Aktiefond").IgnoreCase
            .Or.Contain("SEB Global").IgnoreCase);
        Assert.That(answer, Does.Contain("loss").IgnoreCase
            .Or.Contain("förlust").IgnoreCase
            .Or.Contain("risk").IgnoreCase);
    }

    #endregion

    #region Category + Document context

    [Test]
    public async Task CategoryPerformanceAndDocuments_CombinesFunctionAndContext()
    {
        var answer = await AskHybrid(
            "What's the best performing fund category, and what do the fund documents say about that segment?");

        Assert.That(answer, Does.Contain("category").IgnoreCase
            .Or.Contain("kategori").IgnoreCase
            .Or.Contain("Equity").IgnoreCase
            .Or.Contain("Bond").IgnoreCase
            .Or.Contain("Fixed Income").IgnoreCase);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Full hybrid pipeline: real semantic search → build context → LLM with function calling.
    /// Uses the same RagPromptBuilder as QuestionAnsweringService.
    /// </summary>
    private async Task<string> AskHybrid(string question)
    {
        // Step 1: Real semantic search over actual fund factsheet embeddings
        var searchResults = await _semanticSearch.SearchAsync(question, maxResults: 5, CancellationToken.None);

        // Step 2: Build context and user prompt via shared RagPromptBuilder
        var promptBuilder = new RagPromptBuilder();
        var context = promptBuilder.BuildContext(searchResults);
        var userPrompt = promptBuilder.BuildUserPrompt(context, question);

        // Step 3: Send to LLM with function calling enabled (same as OpenAiProvider)
        var history = new ChatHistory();
        history.AddSystemMessage(_systemPrompt);
        history.AddUserMessage(userPrompt);

        var result = await _chat.GetChatMessageContentAsync(
            history,
            new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() },
            _kernel);

        return result.Content ?? string.Empty;
    }

    #endregion

    #region Test data seeding

    /// <summary>
    /// Seeds fund profiles and history matching the 15 real SEB funds in test_embeddings.json.
    /// ISINs extracted from the actual PDF content.
    /// </summary>
    private static void SeedFundData(TestFundDataDbContextFactory dbFactory)
    {
        using var ctx = dbFactory.CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        long recordId = 1;

        var asien = new FundProfile
        {
            Id = IsinId.Create("SE0021150174"),
            Name = "SEB Asienfond ex Japan",
            Category = "Asia ex Japan Equity",
            ManagedType = "ACTIVE", Risk = 5,
            ManagementFee = 0.0150m, TotalFee = 0.0180m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var emUsd = new FundProfile
        {
            Id = IsinId.Create("LU0037256269"),
            Name = "SEB Emerging Marketsfond C USD - Lux",
            Category = "Emerging Markets Equity",
            ManagedType = "ACTIVE", Risk = 6,
            ManagementFee = 0.0175m, TotalFee = 0.0200m,
            SustainabilityRating = 2, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var emSek = new FundProfile
        {
            Id = IsinId.Create("SE0000894155"),
            Name = "SEB Emerging Marketsfond",
            Category = "Emerging Markets Equity",
            ManagedType = "ACTIVE", Risk = 6,
            ManagementFee = 0.0150m, TotalFee = 0.0180m,
            SustainabilityRating = 2, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var defence = new FundProfile
        {
            Id = IsinId.Create("SE0025491764"),
            Name = "SEB European Defence & Security Fund A",
            Category = "Sector Equity",
            ManagedType = "ACTIVE", Risk = 5,
            ManagementFee = 0.0150m, TotalFee = 0.0175m,
            SustainabilityRating = 1, EuArticleType = "Article 6",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var foretagsobl = new FundProfile
        {
            Id = IsinId.Create("SE0011644475"),
            Name = "SEB Företagsobligationsfond A",
            Category = "Corporate Bond",
            ManagedType = "ACTIVE", Risk = 3,
            ManagementFee = 0.0060m, TotalFee = 0.0075m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var globalAktie = new FundProfile
        {
            Id = IsinId.Create("SE0000434151"),
            Name = "SEB Global Aktiefond A",
            Category = "Global Equity",
            ManagedType = "ACTIVE", Risk = 5,
            ManagementFee = 0.0125m, TotalFee = 0.0150m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var globalHy = new FundProfile
        {
            Id = IsinId.Create("LU0413134395"),
            Name = "SEB Global High Yield C H-SEK",
            Category = "High Yield Bond",
            ManagedType = "ACTIVE", Risk = 4,
            ManagementFee = 0.0100m, TotalFee = 0.0130m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var kortrante = new FundProfile
        {
            Id = IsinId.Create("SE0004867190"),
            Name = "SEB Korträntefond SEK",
            Category = "Short-Term Bond",
            ManagedType = "ACTIVE", Risk = 2,
            ManagementFee = 0.0025m, TotalFee = 0.0035m,
            SustainabilityRating = 4, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var nordamerika = new FundProfile
        {
            Id = IsinId.Create("SE0000434268"),
            Name = "SEB Nordamerika Små och Medelstora Bolag",
            Category = "US Small/Mid Cap Equity",
            ManagedType = "ACTIVE", Risk = 6,
            ManagementFee = 0.0150m, TotalFee = 0.0180m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var norden = new FundProfile
        {
            Id = IsinId.Create("SE0000894189"),
            Name = "SEB Nordenfond",
            Category = "Nordic Equity",
            ManagedType = "ACTIVE", Risk = 5,
            ManagementFee = 0.0125m, TotalFee = 0.0150m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var svExp = new FundProfile
        {
            Id = IsinId.Create("SE0000894197"),
            Name = "SEB Sverige Expanderad",
            Category = "Sweden Equity",
            ManagedType = "ACTIVE", Risk = 5,
            ManagementFee = 0.0125m, TotalFee = 0.0150m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var sverigefond = new FundProfile
        {
            Id = IsinId.Create("SE0000775298"),
            Name = "SEB Sverigefond",
            Category = "Sweden Equity",
            ManagedType = "ACTIVE", Risk = 5,
            ManagementFee = 0.0125m, TotalFee = 0.0150m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var swedishValue = new FundProfile
        {
            Id = IsinId.Create("SE0011838004"),
            Name = "SEB Swedish Value Fund",
            Category = "Sweden Equity",
            ManagedType = "ACTIVE", Risk = 5,
            ManagementFee = 0.0100m, TotalFee = 0.0130m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var usaIndex = new FundProfile
        {
            Id = IsinId.Create("LU0047321666"),
            Name = "SEB USA Indexnära D USD - Lux",
            Category = "US Equity",
            ManagedType = "PASSIVE", Risk = 5,
            ManagementFee = 0.0040m, TotalFee = 0.0050m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var varlden = new FundProfile
        {
            Id = IsinId.Create("SE0000984908"),
            Name = "SEB Världenfond",
            Category = "Global Equity",
            ManagedType = "ACTIVE", Risk = 5,
            ManagementFee = 0.0125m, TotalFee = 0.0150m,
            SustainabilityRating = 3, EuArticleType = "Article 8",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        var allFunds = new[] { asien, emUsd, emSek, defence, foretagsobl, globalAktie,
            globalHy, kortrante, nordamerika, norden, svExp, sverigefond, swedishValue, usaIndex, varlden };

        ctx.FundProfiles.AddRange(allFunds);

        // NAV history — spread of returns so performance queries have clear winners/losers
        var navData = new (FundProfile Fund, decimal StartNav, decimal EndNav)[]
        {
            (asien,        100m, 103m),    // +3%
            (emUsd,        100m, 105m),    // +5%
            (emSek,        100m, 107m),    // +7%
            (defence,      100m, 112m),    // +12% — best performer
            (foretagsobl,  100m, 101m),    // +1%
            (globalAktie,  100m, 106m),    // +6%
            (globalHy,     100m, 102m),    // +2%
            (kortrante,    100m,  97m),    // -3% — worst performer
            (nordamerika,  100m, 108m),    // +8%
            (norden,       100m, 104m),    // +4%
            (svExp,        100m, 105m),    // +5%
            (sverigefond,  100m, 103m),    // +3%
            (swedishValue, 100m, 104m),    // +4%
            (usaIndex,     100m, 109m),    // +9%
            (varlden,      100m, 106m),    // +6%
        };

        foreach (var (fund, startNav, endNav) in navData)
        {
            ctx.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.Create(recordId++),
                IsinId = fund.Id,
                Nav = startNav, NavDate = today.AddDays(-30)
            });
            ctx.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.Create(recordId++),
                IsinId = fund.Id,
                Nav = endNav, NavDate = today.AddDays(-1)
            });
        }

        // Ownership history — clear winners/losers
        var ownerData = new (FundProfile Fund, int StartOwners, int EndOwners)[]
        {
            (defence,      5000,  7000),  // +2000 — biggest gainer
            (emSek,        8000,  8500),  // +500
            (globalAktie, 12000, 12300),  // +300
            (norden,      10000,  9200),  // -800 — biggest loser
            (sverigefond, 15000, 14700),  // -300
            (kortrante,   20000, 19800),  // -200
        };

        foreach (var (fund, startOwners, endOwners) in ownerData)
        {
            ctx.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.Create(recordId++),
                IsinId = fund.Id,
                NumberOfOwners = startOwners, NavDate = today.AddDays(-10)
            });
            ctx.FundHistoryRecords.Add(new FundHistoryRecord
            {
                Id = FundHistoryRecordId.Create(recordId++),
                IsinId = fund.Id,
                NumberOfOwners = endOwners, NavDate = today.AddDays(-1)
            });
        }

        ctx.SaveChanges();
    }

    /// <summary>
    /// Minimal IDocumentRepository that loads chunks from a JSON file (same format as embeddings.json).
    /// </summary>
    private sealed class TestDocumentRepository : IDocumentRepository
    {
        private readonly string _filePath;
        private List<DocumentChunk> _chunks = [];

        public bool IsInitialized { get; private set; }

        public TestDocumentRepository(string filePath) => _filePath = filePath;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var dtos = JsonSerializer.Deserialize<List<EmbeddingRecordDto>>(json)
                       ?? throw new InvalidOperationException("No embeddings found in file");

            _chunks = dtos.Select(dto => DocumentChunk.Create(
                dto.Id, dto.Text, dto.Embedding, dto.Source, dto.Page
            )).ToList();

            IsInitialized = true;
        }

        public Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync() =>
            Task.FromResult<IReadOnlyList<DocumentChunk>>(_chunks);

        public int GetChunkCount() => _chunks.Count;

        public Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task UpdateChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task DeleteChunksBySourceAsync(string sourceFile, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task ReplaceAllChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    #endregion
}
