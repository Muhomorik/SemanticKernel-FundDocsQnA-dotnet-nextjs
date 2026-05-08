using NUnit.Framework;
using YieldRaccoon.Infrastructure.Services;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(FundStatisticsCalculator))]
public class FundStatisticsCalculatorTests
{
    private const string TestIsin = "SE0000000001";
    private const string TestName = "Test Fund";
    private static readonly DateOnly PeriodStart = new(2026, 1, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 1, 14);

    #region Null / Edge Cases

    [Test]
    public void Compute_WithSingleDataPoint_ReturnsNull()
    {
        // Arrange
        var navValues = new[] { 100.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Compute_WithEmptyArray_ReturnsNull()
    {
        // Arrange
        var navValues = Array.Empty<decimal>();

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Compute_WithTwoDataPoints_ReturnsValidStats()
    {
        // Arrange
        var navValues = new[] { 100.0m, 105.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Return2wPct, Is.EqualTo(5.0).Within(0.01));
    }

    #endregion

    #region Total Return

    [Test]
    public void Compute_PositiveReturn_CalculatesCorrectTotalReturn()
    {
        // Arrange — 100 → 110 = 10% return
        var navValues = new[] { 100.0m, 102.0m, 105.0m, 108.0m, 110.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.Return2wPct, Is.EqualTo(10.0).Within(0.01));
    }

    [Test]
    public void Compute_NegativeReturn_CalculatesCorrectTotalReturn()
    {
        // Arrange — 100 → 90 = -10% return
        var navValues = new[] { 100.0m, 98.0m, 95.0m, 92.0m, 90.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.Return2wPct, Is.EqualTo(-10.0).Within(0.01));
    }

    #endregion

    #region NAV High / Low

    [Test]
    public void Compute_ReturnsCorrectNavHighAndLow()
    {
        // Arrange
        var navValues = new[] { 100.0m, 105.0m, 98.0m, 110.0m, 102.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.FirstNav, Is.EqualTo(100.0m));
        Assert.That(result.LastNav, Is.EqualTo(102.0m));
        Assert.That(result.NavHigh, Is.EqualTo(110.0m));
        Assert.That(result.NavLow, Is.EqualTo(98.0m));
    }

    #endregion

    #region Constant NAV

    [Test]
    public void Compute_ConstantNav_ReturnsZeroVolatilityAndNaNSharpe()
    {
        // Arrange — all NAVs are identical → vol = 0, which trips the near-zero-vol guard
        var navValues = new[] { 100.0m, 100.0m, 100.0m, 100.0m, 100.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.AnnVolatility2wPct, Is.EqualTo(0.0));
        Assert.That(double.IsNaN(result.Sharpe2w), Is.True, "Sharpe must be NaN when volatility is below the guard threshold");
        Assert.That(result.Return2wPct, Is.EqualTo(0.0));
        Assert.That(result.BestDayPct, Is.EqualTo(0.0));
        Assert.That(result.WorstDayPct, Is.EqualTo(0.0));
    }

    [Test]
    public void Compute_NearZeroVolatility_ReturnsNaNSharpe()
    {
        // Arrange — minuscule daily moves below the 0.01 % annualized-vol threshold.
        // NAV values 100, 100.000001, 100, 100.000001 produce vol ≈ 1e-6 % — well under 0.01 %.
        var navValues = new[] { 100.0m, 100.000001m, 100.0m, 100.000001m, 100.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.AnnVolatility2wPct, Is.LessThan(0.01));
        Assert.That(double.IsNaN(result.Sharpe2w), Is.True, "Sharpe must be NaN when ann_volatility < 0.01 %");
    }

    #endregion

    #region Volatility

    [Test]
    public void Compute_WithKnownReturns_CalculatesCorrectAnnualizedVolatility()
    {
        // Arrange — NAVs: 100 → 101 → 100 → 101 → 100
        // Daily returns: [+0.01, -0.00990099, +0.01, -0.00990099] (asymmetric due to compounding)
        // Sample std ≈ 0.01149, annualized = 0.01149 * sqrt(252) * 100 ≈ 18.24%
        var navValues = new[] { 100.0m, 101.0m, 100.0m, 101.0m, 100.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.AnnVolatility2wPct, Is.EqualTo(18.24).Within(0.5));
    }

    #endregion

    #region Drawdowns

    [Test]
    public void Compute_MaxDrawdown_CorrectForPeakToTrough()
    {
        // Arrange — peak at 120, trough at 90: drawdown = (90-120)/120 = -25%
        var navValues = new[] { 100.0m, 120.0m, 90.0m, 110.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.MaxDrawdown2wPct, Is.EqualTo(-25.0).Within(0.01));
    }

    [Test]
    public void Compute_CurrentDrawdown_WhenAtPeak_ReturnsZero()
    {
        // Arrange — last NAV is the highest
        var navValues = new[] { 100.0m, 105.0m, 110.0m, 115.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.CurrentDrawdownPct, Is.EqualTo(0.0).Within(0.01));
    }

    [Test]
    public void Compute_CurrentDrawdown_WhenBelowPeak_ReturnsNegative()
    {
        // Arrange — peak at 120, current at 108: (108-120)/120 = -10%
        var navValues = new[] { 100.0m, 120.0m, 110.0m, 108.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.CurrentDrawdownPct, Is.EqualTo(-10.0).Within(0.01));
    }

    #endregion

    #region Sharpe Ratio

    [Test]
    public void Compute_PositiveReturn_PositiveSharpe()
    {
        // Arrange — steady upward movement
        var navValues = new[] { 100.0m, 101.0m, 102.0m, 103.0m, 104.0m, 105.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.Sharpe2w, Is.GreaterThan(0));
    }

    [Test]
    public void Compute_NegativeReturn_NegativeSharpe()
    {
        // Arrange — steady downward movement
        var navValues = new[] { 105.0m, 104.0m, 103.0m, 102.0m, 101.0m, 100.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.Sharpe2w, Is.LessThan(0));
    }

    #endregion

    #region Best / Worst Day

    [Test]
    public void Compute_BestAndWorstDay_ReturnsCorrectValues()
    {
        // Arrange — returns: +5%, -3%, +2%, -1%
        var navValues = new[] { 100.0m, 105.0m, 101.85m, 103.887m, 102.84813m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.BestDayPct, Is.EqualTo(5.0).Within(0.01));
        Assert.That(result.WorstDayPct, Is.EqualTo(-3.0).Within(0.01));
    }

    #endregion

    #region Percentage Positive Days

    [Test]
    public void Compute_PctPositiveDays_CorrectPercentage()
    {
        // Arrange — 3 up days, 2 down days = 60%
        var navValues = new[] { 100.0m, 101.0m, 100.5m, 101.5m, 101.0m, 102.0m };
        // Returns: +1%, -0.5%, +1%, -0.5%, +1% → 3 positive, 2 negative

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.PctPositiveDays, Is.EqualTo(60.0).Within(0.01));
    }

    #endregion

    #region Skewness

    [Test]
    public void Compute_WithThreeOrMoreReturns_CalculatesSkewness()
    {
        // Arrange — asymmetric returns (one large negative, several small positives)
        // This should produce negative skewness
        var navValues = new[] { 100.0m, 101.0m, 102.0m, 103.0m, 90.0m, 91.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert — large drop creates left-skewed distribution
        Assert.That(result.Skewness, Is.LessThan(0));
    }

    [Test]
    public void Compute_WithTwoReturns_ReturnsZeroSkewness()
    {
        // Arrange — only 3 NAV values → 2 returns, not enough for skewness
        var navValues = new[] { 100.0m, 105.0m, 110.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues)!;

        // Assert
        Assert.That(result.Skewness, Is.EqualTo(0.0));
    }

    #endregion

    #region Zero NAV Handling

    [Test]
    public void Compute_WithZeroNavInMiddle_HandlesGracefully()
    {
        // Arrange — a zero NAV in the middle (data quality issue)
        var navValues = new[] { 100.0m, 0.0m, 100.0m, 105.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(TestIsin, TestName, PeriodStart, PeriodEnd, navValues);

        // Assert — should not throw, should return a result
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Period Metadata

    [Test]
    public void Compute_PreservesIsinNameAndPeriodDates()
    {
        // Arrange
        var isin = "SE0012345678";
        var name = "My Test Fund";
        var start = new DateOnly(2026, 2, 1);
        var end = new DateOnly(2026, 2, 14);
        var navValues = new[] { 100.0m, 105.0m, 110.0m };

        // Act
        var result = FundStatisticsCalculator.Compute(isin, name, start, end, navValues)!;

        // Assert
        Assert.That(result.Isin, Is.EqualTo(isin));
        Assert.That(result.Name, Is.EqualTo(name));
        Assert.That(result.PeriodStart, Is.EqualTo(start));
        Assert.That(result.PeriodEnd, Is.EqualTo(end));
    }

    #endregion
}
