using System.ComponentModel;

using Backend.API.Infrastructure.FundData;

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
