using MathNet.Numerics.Statistics;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Computes summary statistics from an ordered series of daily NAV values.
/// Pure math — no I/O, no dependencies beyond MathNet.Numerics.
/// </summary>
internal static class FundStatisticsCalculator
{
    private const int TradingDaysPerYear = 252;

    /// <summary>
    /// Computes 13 summary statistics for a single time window of NAV data.
    /// </summary>
    /// <param name="isin">Fund ISIN identifier.</param>
    /// <param name="name">Fund display name.</param>
    /// <param name="periodStart">First date in the time window.</param>
    /// <param name="periodEnd">Last date in the time window.</param>
    /// <param name="navValues">Daily NAV values sorted chronologically (oldest first). Must have at least 2 elements.</param>
    /// <returns>Computed summary statistics, or <c>null</c> if fewer than 2 data points.</returns>
    public static FundSummaryStatistics? Compute(
        string isin,
        string name,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal[] navValues)
    {
        if (navValues.Length < 2)
            return null;

        var firstNav = navValues[0];
        var lastNav = navValues[^1];
        var navHigh = navValues.Max();
        var navLow = navValues.Min();

        // Daily returns: (nav[i] / nav[i-1]) - 1
        var dailyReturns = new double[navValues.Length - 1];
        for (var i = 1; i < navValues.Length; i++)
        {
            var prev = (double)navValues[i - 1];
            dailyReturns[i - 1] = prev == 0 ? 0 : (double)navValues[i] / prev - 1;
        }

        // Total return
        var totalReturnPct = firstNav == 0 ? 0 : (double)(lastNav / firstNav - 1) * 100;

        // Annualized volatility = std(daily_returns) × √252 × 100
        var dailyStdDev = dailyReturns.StandardDeviation();
        var annVolatility = dailyStdDev * Math.Sqrt(TradingDaysPerYear) * 100;

        // Drawdowns
        var (maxDrawdownPct, currentDrawdownPct) = ComputeDrawdowns(navValues);

        // Sharpe ratio (risk-free rate = 0): ann_return / ann_volatility
        var annReturn = dailyReturns.Mean() * TradingDaysPerYear;
        var sharpeRatio = dailyStdDev == 0
            ? 0
            : annReturn / (dailyStdDev * Math.Sqrt(TradingDaysPerYear));

        // Best/worst single-day return
        var bestDayPct = dailyReturns.Max() * 100;
        var worstDayPct = dailyReturns.Min() * 100;

        // Percentage of positive days
        var positiveDays = 0;
        foreach (var r in dailyReturns)
        {
            if (r > 0) positiveDays++;
        }

        var pctPositiveDays = (double)positiveDays / dailyReturns.Length * 100;

        // Skewness
        var skewness = dailyReturns.Length < 3 ? 0 : dailyReturns.Skewness();

        return new FundSummaryStatistics(
            Isin: isin,
            Name: name,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            FirstNav: firstNav,
            LastNav: lastNav,
            NavHigh: navHigh,
            NavLow: navLow,
            TotalReturnPct: totalReturnPct,
            AnnVolatility: annVolatility,
            MaxDrawdownPct: maxDrawdownPct,
            CurrentDrawdownPct: currentDrawdownPct,
            SharpeRatio: sharpeRatio,
            BestDayPct: bestDayPct,
            WorstDayPct: worstDayPct,
            PctPositiveDays: pctPositiveDays,
            Skewness: skewness);
    }

    private static (double maxDrawdownPct, double currentDrawdownPct) ComputeDrawdowns(decimal[] navValues)
    {
        var runningMax = navValues[0];
        var maxDrawdown = 0.0;

        foreach (var nav in navValues)
        {
            if (nav > runningMax)
                runningMax = nav;

            if (runningMax > 0)
            {
                var drawdown = (double)(nav - runningMax) / (double)runningMax;
                if (drawdown < maxDrawdown)
                    maxDrawdown = drawdown;
            }
        }

        // Current drawdown: distance from period peak
        var peak = navValues.Max();
        var currentDrawdown = peak > 0
            ? (double)(navValues[^1] - peak) / (double)peak * 100
            : 0;

        return (maxDrawdown * 100, currentDrawdown);
    }
}
