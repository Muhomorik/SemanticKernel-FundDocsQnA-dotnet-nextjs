using System.Globalization;

using Backend.API.ApplicationCore.DTOs.OwnershipFlow;
using Backend.API.ApplicationCore.Services;
using Backend.API.Domain.FundData.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.API.Infrastructure.FundData.Services;

/// <summary>
/// Computes ownership flow data for Sankey chart visualization.
/// Queries FundDataDbContext directly via IDbContextFactory (read-only, no-tracking).
/// </summary>
public sealed class OwnershipFlowService : IOwnershipFlowService
{
    private const int WeekCount = 4;
    private const int MaxMonths = 3;
    private const int TopFundsPerSide = 10;
    private const int MinOwners = 100;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IDbContextFactory<FundDataDbContext> _contextFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OwnershipFlowService> _logger;

    public OwnershipFlowService(
        IDbContextFactory<FundDataDbContext> contextFactory,
        IMemoryCache cache,
        ILogger<OwnershipFlowService> logger)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public OwnershipFlowPeriodsResponse GetAvailablePeriods()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var weekly = BuildWeeklyPeriods(today);
        var monthly = BuildMonthlyPeriods(today);

        return new OwnershipFlowPeriodsResponse(weekly, monthly);
    }

    /// <inheritdoc />
    public async Task<OwnershipFlowResponse> GetOwnershipFlowAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ownership-flow:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}";

        if (_cache.TryGetValue(cacheKey, out OwnershipFlowResponse? cached) && cached is not null)
        {
            _logger.LogDebug("Ownership flow cache hit for {From} to {To}", from, to);
            return cached;
        }

        _logger.LogInformation("Computing ownership flow for {From} to {To}", from, to);

        var result = await ComputeOwnershipFlowAsync(from, to, cancellationToken);

        _cache.Set(cacheKey, result, CacheDuration);

        return result;
    }

    private async Task<OwnershipFlowResponse> ComputeOwnershipFlowAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        // Query 1: all records with non-null owners up to `to` (no lower bound —
        // we look back before `from` to find the baseline for each fund).
        var records = await context.FundHistoryRecords
            .Where(r => r.NavDate != null
                     && r.NavDate <= to
                     && r.NumberOfOwners != null)
            .Select(r => new { r.IsinId, r.NumberOfOwners, r.NavDate })
            .ToListAsync(ct);

        // Query 2: Fund profiles with >= MinOwners (for filtering + name/category lookup)
        var profiles = await context.FundProfiles
            .Where(fp => fp.NumberOfOwners != null && fp.NumberOfOwners >= MinOwners)
            .Select(fp => new { fp.Id, fp.Name, fp.Category })
            .ToDictionaryAsync(fp => fp.Id.Isin, ct);

        _logger.LogDebug("Loaded {RecordCount} history records, {ProfileCount} qualifying profiles",
            records.Count, profiles.Count);

        // In-memory: look-back delta calculation.
        // For each fund:
        //   startRecord = most recent snapshot at or before `from`  (the look-back baseline)
        //   endRecord   = most recent snapshot at or before `to`
        // Using this approach, longer date ranges pick up earlier baselines and
        // produce genuinely different deltas — monthly periods no longer return
        // identical data when the DB has sparse NumberOfOwners history.
        var fundDeltas = new List<(string Name, string? Category, int Delta, double Pct, int StartOwners)>();

        foreach (var group in records.GroupBy(r => r.IsinId.Isin))
        {
            if (!profiles.TryGetValue(group.Key, out var profile)) continue;

            var ordered = group.OrderByDescending(r => r.NavDate).ToList();
            var endRecord   = ordered[0];                                          // most recent ≤ to
            var startRecord = ordered.FirstOrDefault(r => r.NavDate <= from)     // most recent ≤ from
                           ?? ordered.LastOrDefault();                             // fallback: earliest available

            // Need two distinct snapshots to compute a meaningful delta
            if (startRecord is null || startRecord.NavDate == endRecord.NavDate) continue;

            var startOwners = startRecord.NumberOfOwners!.Value;
            var endOwners   = endRecord.NumberOfOwners!.Value;
            var delta       = endOwners - startOwners;
            if (delta == 0) continue;

            var pct = startOwners != 0
                ? Math.Round(delta / (double)startOwners * 100, 1)
                : 0.0;

            fundDeltas.Add((profile.Name, profile.Category, delta, pct, startOwners));
        }

        // Fund-level chart: top 10 per direction
        var fundOut = fundDeltas
            .Where(f => f.Delta < 0)
            .OrderBy(f => f.Delta)
            .Take(TopFundsPerSide)
            .Select(f => new OwnershipFlowItem(f.Name, Math.Abs(f.Delta), f.Pct))
            .ToList();

        var fundIn = fundDeltas
            .Where(f => f.Delta > 0)
            .OrderByDescending(f => f.Delta)
            .Take(TopFundsPerSide)
            .Select(f => new OwnershipFlowItem(f.Name, f.Delta, f.Pct))
            .ToList();

        // Category-level chart: aggregate ALL qualifying funds by macro-group
        var catGroups = fundDeltas
            .GroupBy(f => CategoryMacroGroup.Resolve(f.Category))
            .Select(g =>
            {
                var totalDelta = g.Sum(f => f.Delta);
                var totalStartOwners = g.Sum(f => f.StartOwners);
                var pct = totalStartOwners != 0
                    ? Math.Round(totalDelta / (double)totalStartOwners * 100, 1)
                    : 0.0;
                return new { Name = g.Key, Delta = totalDelta, Pct = pct };
            })
            .Where(c => c.Delta != 0)
            .ToList();

        var catOut = catGroups
            .Where(c => c.Delta < 0)
            .OrderBy(c => c.Delta)
            .Select(c => new OwnershipFlowItem(c.Name, Math.Abs(c.Delta), c.Pct))
            .ToList();

        var catIn = catGroups
            .Where(c => c.Delta > 0)
            .OrderByDescending(c => c.Delta)
            .Select(c => new OwnershipFlowItem(c.Name, c.Delta, c.Pct))
            .ToList();

        var periodLabel = FormatPeriodLabel(from, to);

        _logger.LogInformation(
            "Ownership flow computed: {FundOut} fund outflows, {FundIn} fund inflows, {CatOut} category outflows, {CatIn} category inflows",
            fundOut.Count, fundIn.Count, catOut.Count, catIn.Count);

        return new OwnershipFlowResponse(
            PeriodLabel: periodLabel,
            Cat: new OwnershipFlowGroup(catOut, catIn),
            Fund: new OwnershipFlowGroup(fundOut, fundIn));
    }

    // ─── Period Helpers ─────────────────────────────────────────────────────────

    private static IReadOnlyList<TimePeriod> BuildWeeklyPeriods(DateOnly today)
    {
        // ISO 8601: Monday = start of week
        var currentMonday = GetMonday(today);

        var periods = new List<TimePeriod>(WeekCount);

        // Current (possibly partial) week: Monday → today
        periods.Add(MakeWeekPeriod(currentMonday, today));

        // Preceding full weeks
        for (var i = 1; i < WeekCount; i++)
        {
            var monday = currentMonday.AddDays(-7 * i);
            var sunday = monday.AddDays(6);
            periods.Add(MakeWeekPeriod(monday, sunday));
        }

        // Reverse so oldest is first (matches mockup order)
        periods.Reverse();

        return periods;
    }

    private static IReadOnlyList<TimePeriod> BuildMonthlyPeriods(DateOnly today)
    {
        var periods = new List<TimePeriod>(MaxMonths);

        for (var months = 1; months <= MaxMonths; months++)
        {
            var from = today.AddMonths(-months);
            var label = months == 1 ? "1 month" : $"{months} months";
            periods.Add(new TimePeriod(label, Format(from), Format(today)));
        }

        return periods;
    }

    private static TimePeriod MakeWeekPeriod(DateOnly from, DateOnly to)
    {
        var label = FormatPeriodLabel(from, to);
        return new TimePeriod(label, Format(from), Format(to));
    }

    /// <summary>
    /// Formats a date range as a human-readable label.
    /// Same month: "Feb 10 – 16". Cross-month: "Feb 24 – Mar 2".
    /// </summary>
    internal static string FormatPeriodLabel(DateOnly from, DateOnly to)
    {
        var fromMonth = from.ToString("MMM", CultureInfo.InvariantCulture);
        var toMonth = to.ToString("MMM", CultureInfo.InvariantCulture);

        if (fromMonth == toMonth)
            return $"{fromMonth} {from.Day} – {to.Day}";

        return $"{fromMonth} {from.Day} – {toMonth} {to.Day}";
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.AddDays(-daysFromMonday);
    }

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd");
}
