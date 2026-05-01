using NUnit.Framework;
using YieldRaccoon.Infrastructure.Services;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(FundSnapshotStatisticsCalculator))]
public class FundSnapshotStatisticsCalculatorTests
{
    private const string TestIsin = "SE0000000001";
    private static readonly DateOnly AsOfDate = new(2026, 4, 30);

    [Test]
    public void Compute_EmptySlices_ReturnsAllNaN()
    {
        var result = FundSnapshotStatisticsCalculator.Compute(TestIsin, AsOfDate,
            slice12w: [], slice1y: []);

        Assert.That(result.Isin, Is.EqualTo(TestIsin));
        Assert.That(result.AsOfDate, Is.EqualTo(AsOfDate));
        Assert.That(double.IsNaN(result.Return12wCompoundPct), Is.True);
        Assert.That(double.IsNaN(result.AnnVolatility12wPct), Is.True);
        Assert.That(double.IsNaN(result.Sharpe12w), Is.True);
        Assert.That(double.IsNaN(result.MaxDrawdown12wPct), Is.True);
        Assert.That(double.IsNaN(result.Return1yCompoundPct), Is.True);
        Assert.That(double.IsNaN(result.AnnVolatility1yPct), Is.True);
        Assert.That(double.IsNaN(result.Sharpe1y), Is.True);
        Assert.That(double.IsNaN(result.MaxDrawdown1yPct), Is.True);
    }

    [Test]
    public void Compute_PopulatedSlices_ProducesFiniteMetrics()
    {
        var slice12w = MakeUpwardSeries(start: AsOfDate.AddDays(-84), days: 84, startNav: 100m, dailyDrift: 0.001m);
        var slice1y = MakeUpwardSeries(start: AsOfDate.AddDays(-365), days: 365, startNav: 100m, dailyDrift: 0.0005m);

        var result = FundSnapshotStatisticsCalculator.Compute(TestIsin, AsOfDate, slice12w, slice1y);

        Assert.That(double.IsNaN(result.Return12wCompoundPct), Is.False);
        Assert.That(result.Return12wCompoundPct, Is.GreaterThan(0));
        Assert.That(double.IsNaN(result.Return1yCompoundPct), Is.False);
        Assert.That(result.Return1yCompoundPct, Is.GreaterThan(0));
    }

    [Test]
    public void Compute_ConstantNavSlice_ReturnsNaNSharpe()
    {
        var flat = MakeFlatSeries(start: AsOfDate.AddDays(-84), days: 84, nav: 100m);

        var result = FundSnapshotStatisticsCalculator.Compute(TestIsin, AsOfDate, slice12w: flat, slice1y: []);

        Assert.That(result.AnnVolatility12wPct, Is.EqualTo(0.0));
        Assert.That(double.IsNaN(result.Sharpe12w), Is.True, "Vol below 0.01 % must produce NaN Sharpe");
    }

    [Test]
    public void Compute_OnlyShortHorizonProvided_OneHorizonIsNaN_OtherIsFinite()
    {
        var slice12w = MakeUpwardSeries(start: AsOfDate.AddDays(-84), days: 84, startNav: 100m, dailyDrift: 0.001m);

        var result = FundSnapshotStatisticsCalculator.Compute(TestIsin, AsOfDate, slice12w: slice12w, slice1y: []);

        Assert.That(double.IsNaN(result.Return12wCompoundPct), Is.False);
        Assert.That(double.IsNaN(result.Return1yCompoundPct), Is.True);
    }

    private static List<(DateOnly date, decimal nav)> MakeUpwardSeries(DateOnly start, int days, decimal startNav, decimal dailyDrift)
    {
        var list = new List<(DateOnly, decimal)>(days);
        var nav = startNav;
        for (var i = 0; i < days; i++)
        {
            list.Add((start.AddDays(i), nav));
            nav += dailyDrift;
        }
        return list;
    }

    private static List<(DateOnly date, decimal nav)> MakeFlatSeries(DateOnly start, int days, decimal nav)
    {
        var list = new List<(DateOnly, decimal)>(days);
        for (var i = 0; i < days; i++)
            list.Add((start.AddDays(i), nav));
        return list;
    }
}
