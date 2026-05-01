using MathNet.Numerics.Statistics;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Computes rolling-horizon statistics (12-week and 1-year) from a fund's NAV series.
/// Pure math — no I/O, no dependencies beyond MathNet.Numerics.
/// </summary>
internal static class FundSnapshotStatisticsCalculator
{
    private const int TradingDaysPerYear = 252;

    /// <summary>
    /// Below this annualized-volatility level (expressed on the *_pct scale), the Sharpe denominator
    /// approaches zero and the ratio explodes. Setting Sharpe to NaN avoids spurious +40 readings on
    /// near-constant NAV series (typically money-market / bond funds). 0.01 means 0.01 percent, not 1 percent.
    /// </summary>
    private const double NearZeroVolatilityThresholdPct = 0.01;

    /// <summary>
    /// Computes 12-week and 1-year rolling-horizon statistics anchored at <paramref name="asOfDate"/>.
    /// Each NAV slice covers the trailing 84 / 365 calendar days respectively. Pass an empty list to
    /// signal "insufficient history" — the corresponding four columns will all be <see cref="double.NaN"/>.
    /// </summary>
    public static FundSnapshotStatistics Compute(
        string isin,
        DateOnly asOfDate,
        IReadOnlyList<(DateOnly date, decimal nav)> slice12w,
        IReadOnlyList<(DateOnly date, decimal nav)> slice1y)
    {
        var (ret12, vol12, sharpe12, mdd12) = ComputeHorizon(slice12w);
        var (ret1y, vol1y, sharpe1y, mdd1y) = ComputeHorizon(slice1y);

        return new FundSnapshotStatistics(
            Isin: isin,
            AsOfDate: asOfDate,
            Return12wCompoundPct: ret12,
            AnnVolatility12wPct: vol12,
            Sharpe12w: sharpe12,
            MaxDrawdown12wPct: mdd12,
            Return1yCompoundPct: ret1y,
            AnnVolatility1yPct: vol1y,
            Sharpe1y: sharpe1y,
            MaxDrawdown1yPct: mdd1y);
    }

    private static (double returnPct, double annVolPct, double sharpe, double maxDdPct) ComputeHorizon(
        IReadOnlyList<(DateOnly date, decimal nav)> slice)
    {
        if (slice.Count < 2)
            return (double.NaN, double.NaN, double.NaN, double.NaN);

        var firstNav = slice[0].nav;
        var lastNav = slice[^1].nav;

        var dailyReturns = new double[slice.Count - 1];
        for (var i = 1; i < slice.Count; i++)
        {
            var prev = (double)slice[i - 1].nav;
            dailyReturns[i - 1] = prev == 0 ? 0 : (double)slice[i].nav / prev - 1;
        }

        var compoundReturnPct = firstNav == 0 ? double.NaN : (double)(lastNav / firstNav - 1) * 100;

        var dailyStdDev = dailyReturns.StandardDeviation();
        var annVolPct = dailyStdDev * Math.Sqrt(TradingDaysPerYear) * 100;

        var annReturn = dailyReturns.Mean() * TradingDaysPerYear;
        var sharpe = annVolPct < NearZeroVolatilityThresholdPct
            ? double.NaN
            : annReturn / (dailyStdDev * Math.Sqrt(TradingDaysPerYear));

        var maxDdPct = ComputeMaxDrawdownPct(slice);

        return (compoundReturnPct, annVolPct, sharpe, maxDdPct);
    }

    private static double ComputeMaxDrawdownPct(IReadOnlyList<(DateOnly date, decimal nav)> slice)
    {
        var runningMax = slice[0].nav;
        var maxDrawdown = 0.0;

        foreach (var (_, nav) in slice)
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

        return maxDrawdown * 100;
    }
}
