using System.ComponentModel;
using System.Text.RegularExpressions;

using Backend.API.Infrastructure.FundData;
using Backend.API.Infrastructure.FundData.Plugins.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace Backend.API.Infrastructure.FundData.Plugins;

/// <summary>
/// Semantic Kernel native plugin exposing fund data queries as kernel functions.
/// The LLM uses function calling to invoke these methods based on user questions.
/// </summary>
/// <remarks>
/// Queries <see cref="FundDataDbContext"/> directly via <see cref="IDbContextFactory{TContext}"/>
/// rather than through domain repositories (which are write-only).
/// Each function call creates a short-lived, no-tracking DbContext.
/// </remarks>
public class FundDataPlugin
{
    private readonly IDbContextFactory<FundDataDbContext> _contextFactory;

    public FundDataPlugin(IDbContextFactory<FundDataDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [KernelFunction("get_available_categories")]
    [Description("Gets a list of all available fund categories. Use this to discover valid category names before filtering by category.")]
    public async Task<string[]> GetAvailableCategoriesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return await context.FundProfiles
            .Where(fp => fp.Category != null)
            .Select(fp => fp.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToArrayAsync();
    }

    [KernelFunction("get_fund_profile")]
    [Description("Gets detailed profile information for a single fund. Search by ISIN code (e.g. SE0008613939) or by fund name (partial match). Returns fees, risk, ESG scores, sustainability ratings, and other metadata.")]
    public async Task<FundProfileResult?> GetFundProfileAsync(
        [Description("Fund name (partial, case-insensitive) or ISIN code (12 characters, e.g. SE0008613939)")] string nameOrIsin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var isIsin = Regex.IsMatch(nameOrIsin, @"^[A-Z]{2}[A-Z0-9]{9}[0-9]$");

        var fund = isIsin
            ? await context.FundProfiles.FirstOrDefaultAsync(fp => fp.Id.Isin == nameOrIsin)
            : await context.FundProfiles.FirstOrDefaultAsync(fp => fp.Name.Contains(nameOrIsin));

        if (fund is null) return null;

        return new FundProfileResult(
            Isin: fund.Id.Isin,
            Name: fund.Name,
            Category: fund.Category,
            CompanyName: fund.CompanyName,
            ManagedType: fund.ManagedType,
            Risk: fund.Risk,
            ManagementFee: fund.ManagementFee,
            TotalFee: fund.TotalFee,
            EsgScore: fund.EsgScore,
            SustainabilityRating: fund.SustainabilityRating,
            SustainabilityLevel: fund.SustainabilityLevel,
            EnvironmentalScore: fund.EnvironmentalScore,
            SocialScore: fund.SocialScore,
            GovernanceScore: fund.GovernanceScore,
            EuArticleType: fund.EuArticleType,
            NumberOfOwners: fund.NumberOfOwners,
            Capital: fund.Capital,
            Rating: fund.Rating,
            CurrencyCode: fund.CurrencyCode);
    }

    [KernelFunction("search_funds")]
    [Description("Search for funds matching multiple optional criteria. All provided filters are combined with AND logic. Returns up to 'limit' matching funds.")]
    public async Task<FundSearchResult[]> SearchFundsAsync(
        [Description("Optional fund name substring to filter by (case-insensitive)")] string? name = null,
        [Description("Optional fund category to filter by (e.g. 'Equity', 'Fixed Income', 'Emerging Markets')")] string? category = null,
        [Description("Optional maximum risk level (1-7, inclusive). Funds with risk <= this value are returned.")] int? maxRisk = null,
        [Description("Optional management type filter: 'ACTIVE' or 'PASSIVE'")] string? managedType = null,
        [Description("Optional minimum sustainability rating (1-5). Funds with rating >= this value are returned.")] int? minSustainabilityRating = null,
        [Description("Optional EU SFDR article type filter (e.g. 'Article 8', 'Article 9')")] string? euArticleType = null,
        [Description("Maximum number of results to return")] int limit = QueryLimits.SearchResults)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var query = context.FundProfiles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(fp => fp.Name.Contains(name));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(fp => fp.Category != null && fp.Category.Contains(category));

        if (maxRisk.HasValue)
            query = query.Where(fp => fp.Risk != null && fp.Risk <= maxRisk.Value);

        if (!string.IsNullOrWhiteSpace(managedType))
            query = query.Where(fp => fp.ManagedType != null && fp.ManagedType.ToUpper() == managedType.ToUpper());

        if (minSustainabilityRating.HasValue)
            query = query.Where(fp => fp.SustainabilityRating != null && fp.SustainabilityRating >= minSustainabilityRating.Value);

        if (!string.IsNullOrWhiteSpace(euArticleType))
            query = query.Where(fp => fp.EuArticleType != null && fp.EuArticleType.Contains(euArticleType));

        return await query
            .OrderBy(fp => fp.Name)
            .Take(limit)
            .Select(fp => new FundSearchResult(
                fp.Id.Isin,
                fp.Name,
                fp.Category,
                fp.Risk,
                fp.ManagedType,
                fp.SustainabilityRating,
                fp.ManagementFee,
                fp.TotalFee,
                fp.EuArticleType))
            .ToArrayAsync();
    }
}

/// <summary>
/// Caps how many results each <see cref="FundDataPlugin"/> function returns to the LLM.
/// Keeping results compact avoids blowing through token budgets.
/// </summary>
public static class QueryLimits
{
    public const int TopPerformingFunds = 10;
    public const int FundsByOwnerChange = 10;
    public const int CategoriesPerformance = 20;
    public const int SearchResults = 10;
}
